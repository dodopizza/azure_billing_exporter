using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AzureBillingExporter.AzureApiAccessToken;
using DotLiquid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureBillingExporter
{
    public class AzureRestApiClient
    {
        private readonly IAccessTokenProvider _accessTokenProvider;
        private readonly ILogger<AzureRestApiClient> _logger;
        
        private ApiSettings ApiSettings { get; }

        public AzureRestApiClient(
            ApiSettings apiSettings,
            IAccessTokenProvider accessTokenProvider, 
            ILogger<AzureRestApiClient> logger)
        {
            _accessTokenProvider = accessTokenProvider;
            _logger = logger;
            ApiSettings = apiSettings;
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
        
        public async Task<IAsyncEnumerable<CostResultRows>> GetMonthlyData(CancellationToken cancel)
        {
            var dateTimeNow = DateTime.Now;
            
            var dateStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, 1);
            dateStart = dateStart.AddMonths(-2);
            var dateEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            var granularity = "Monthly";

            var billingQuery = await GenerateBillingQuery(dateStart, dateEnd, granularity);
            return ExecuteBillingQuery(billingQuery, cancel);
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
            
            var weekAgo = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
            weekAgo = weekAgo.AddDays(-7);

            return template.Render(Hash.FromAnonymousObject(new
            {
                DayStart = dateStart.ToString("o", CultureInfo.InvariantCulture), 
                DayEnd = dateEnd.ToString("o", CultureInfo.InvariantCulture),
                CurrentMonthStart = currentMonthStart.ToString("o", CultureInfo.InvariantCulture),
                PrevMonthStart = prevMonthStart.ToString("o", CultureInfo.InvariantCulture),
                TodayEnd = todayEnd.ToString("o", CultureInfo.InvariantCulture),
                YesterdayStart = yesterdayStart.ToString("o", CultureInfo.InvariantCulture),
                WeekAgo = weekAgo.ToString("o", CultureInfo.InvariantCulture),
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
            
            var accessToken = _accessTokenProvider.GetAccessToken();
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.SendAsync(request, cancel);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception($"{response.StatusCode} {await response.Content.ReadAsStringAsync()}");
            }
            
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