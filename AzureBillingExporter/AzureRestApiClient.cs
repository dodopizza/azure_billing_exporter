using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using DotLiquid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureBillingExporter
{
    public class AzureRestApiClient
    {
        private readonly ILogger<AzureRestApiClient> _logger;
        
        private ApiSettings ApiSettings { get; }

        private string BearerToken {get;}

        public AzureRestApiClient(ApiSettings apiSettings, ILogger<AzureRestApiClient> logger)
        {
            _logger = logger;
            ApiSettings = apiSettings;
            BearerToken = GetBearerToken();
        }

        private string GetBearerToken()
        {
            _logger.LogTrace("Into GetBearerToken");
            var azureAdUrl = $"https://login.microsoftonline.com/{ApiSettings.TenantId}/oauth2/token";

            var resourceEncoded = HttpUtility.UrlEncode("https://management.azure.com");
            var clientSecretEncoded = HttpUtility.UrlEncode(ApiSettings.ClientSecret);
            var bodyStr =
                $"grant_type=client_credentials&client_id={ApiSettings.ClientId}&client_secret={clientSecretEncoded}&resource={resourceEncoded}";

            _logger.LogTrace($"Bearer request bodyStr", bodyStr);
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false
            };
            using var httpClient = new HttpClient(handler);
            
            var response = httpClient.PostAsync(azureAdUrl, 
                new StringContent(
                    bodyStr, 
                    Encoding.UTF8, 
                    "application/x-www-form-urlencoded")).Result;

            var responseContent = response.Content.ReadAsStringAsync().Result;
            _logger.LogTrace($"Bearer response content {responseContent}");
            var result = JsonConvert.DeserializeObject<AzureAdResult>(responseContent); 

            return result.access_token;
        }

        public async Task<IAsyncEnumerable<CostResultRows>> GetCustomData(CancellationToken cancel, string templateFileName)
        {
            var billingQuery = await GenerateBillingQuery(DateTime.MaxValue, DateTime.MaxValue, "None", templateFileName);
            return ExecuteBillingQuery(billingQuery, cancel);
        }
        
        public async Task<IAsyncEnumerable<CostResultRows>> GetDailyData(CancellationToken cancel)
        {
            var dateTimeNow = DateTime.Now;
            var dateStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
            dateStart = dateStart.AddDays(-2);
            var dateEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            var granularity = "Daily";

            var billingQuery = await GenerateBillingQuery(dateStart, dateEnd, granularity);
            return ExecuteBillingQuery(billingQuery, cancel);
        }
        
        public async Task<CostResultRows> GetMonthlyData(CancellationToken cancel)
        {
            var dateTimeNow = DateTime.Now;
            
            var dateStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);
            var dateEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            var granularity = "Monthly";

            var billingQuery = await GenerateBillingQuery(dateStart, dateEnd, granularity);
            await foreach (var monthData in ExecuteBillingQuery(billingQuery, cancel).WithCancellation(cancel))
            {
                return monthData;
            }

            return null;
        }

        private async Task<string> GenerateBillingQuery(DateTime dateStart, DateTime dateEnd, string granularity = "None", string templateFile = "./queries/get_daily_or_monthly_costs.json")
        {
            var dateTimeNow = DateTime.Now;
            
            var templateQuery =await File.ReadAllTextAsync(templateFile);
            var template = Template.Parse(templateQuery);
            
            var currentMonthStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);
            var prevMonthStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);
            prevMonthStart = prevMonthStart.AddMonths(-1);
            
            var todayEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            
            var yesterdayStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
            yesterdayStart = yesterdayStart.AddDays(-1);

            return template.Render(Hash.FromAnonymousObject(new
            {
                DayStart = dateStart.ToString("o", CultureInfo.InvariantCulture), 
                DayEnd = dateEnd.ToString("o", CultureInfo.InvariantCulture),
                CurrentMonthStart = currentMonthStart.ToString("o", CultureInfo.InvariantCulture),
                PrevMonthStart = prevMonthStart.ToString("o", CultureInfo.InvariantCulture),
                TodayEnd = todayEnd.ToString("o", CultureInfo.InvariantCulture),
                YesterdayStart = yesterdayStart.ToString("o", CultureInfo.InvariantCulture),
                Granularity = granularity
            }));
        }

        private async IAsyncEnumerable<CostResultRows> ExecuteBillingQuery(string billingQuery, [EnumeratorCancellation] CancellationToken cancel)
        {
            var azureManagementUrl =
                $"https://management.azure.com/subscriptions/{ApiSettings.SubscriptionId}/providers/Microsoft.CostManagement/query?api-version=2019-10-01";

            using var httpClient = new HttpClient();

            _logger.LogTrace($"Billing query {billingQuery}");
            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(azureManagementUrl),
                Method = HttpMethod.Post,
                Content = new StringContent(
                    billingQuery,
                    Encoding.UTF8,
                    "application/json")
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", BearerToken);

            var response = await httpClient.SendAsync(request, cancel);

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogTrace($"Billing query response result {responseContent}");
            dynamic json = JsonConvert.DeserializeObject(responseContent);
            
            foreach (var row in json.properties.rows)
            {
                yield return CostResultRows.Cast(json.properties.columns, row);
            }
        }
    }
}