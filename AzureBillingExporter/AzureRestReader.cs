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
using Newtonsoft.Json;

namespace AzureBillingExporter
{
    public class ApiSettings
    {
        public string SubsriptionId { get; set; }
        public string TenantId  { get; set; }
        public string ClientId  { get; set; }
        public string ClientSecret  { get; set; }
    }

    public class AzureAdResult
    {
        public string access_token { get; set; }
    }

    public class AzureRestReader
    {
        private ApiSettings ApiSettings { get; }

        private string BearerToken {get;}

        public AzureRestReader()
        {
            var secretFilePath = ".secrets/billing_reader_sp.json";
            using var r = new StreamReader(secretFilePath);
            var json = r.ReadToEnd();
            var settings = JsonConvert.DeserializeObject<ApiSettings>(json);

            ApiSettings = settings;
            BearerToken = GetBearerToken();
        }

        private string GetBearerToken()
        {
            var azureAdUrl = $"https://login.microsoftonline.com/{ApiSettings.TenantId}/oauth2/token";

            var resourceEncoded = HttpUtility.UrlEncode("https://management.azure.com");
            var clientSecretEncoded = HttpUtility.UrlEncode(ApiSettings.ClientSecret);
            var bodyStr =
                $"grant_type=client_credentials&client_id={ApiSettings.ClientId}&client_secret={clientSecretEncoded}&resource={resourceEncoded}";

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

            var result = JsonConvert.DeserializeObject<AzureAdResult>(response.Content.ReadAsStringAsync().Result); 

            return result.access_token;
        }

        public async Task<IAsyncEnumerable<CostResultRows>> GetCustomData(CancellationToken cancel, string templateFileName)
        {
            var dateTimeNow = DateTime.Now;
            var dateStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day);
            var dateEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            const string granularity = "Daily";

            var billingQuery = await GenerateBillingQuery(dateStart, dateEnd, granularity, templateFileName);
            return ExecuteBillingQuery(billingQuery, cancel);
        }
        
        public async Task<IAsyncEnumerable<CostResultRows>> GetDailyData(CancellationToken cancel)
        {
            var dateTimeNow = DateTime.Now;
            var dateStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day - 2);
            var dateEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day, 23, 59, 59);
            const string granularity = "Daily";

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

        private static async Task<string> GenerateBillingQuery(DateTime dateStart, DateTime dateEnd, string granularity, string templateFile = "./queries/get_daily_monthly_costs.json")
        {
            var templateQuery =await File.ReadAllTextAsync(templateFile);
            var template = Template.Parse(templateQuery);
            return await Task.Run(()=>template.Render(Hash.FromAnonymousObject(new
            {
                DayStart = dateStart.ToString("o", CultureInfo.InvariantCulture), 
                DayEnd = dateEnd.ToString("o", CultureInfo.InvariantCulture),
                Granularity = granularity
            })));
        }

        private async IAsyncEnumerable<CostResultRows> ExecuteBillingQuery(string billingQuery, [EnumeratorCancellation] CancellationToken cancel)
        {
            var azureManagementUrl =
                $"https://management.azure.com/subscriptions/{ApiSettings.SubsriptionId}/providers/Microsoft.CostManagement/query?api-version=2019-10-01";

            using var httpClient = new HttpClient();

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

            dynamic json = JsonConvert.DeserializeObject(await response.Content.ReadAsStringAsync());
            
            foreach (var row in json.properties.rows)
            {
                yield return CostResultRows.Cast(json.properties.columns, row);
            }
        }
    }
}