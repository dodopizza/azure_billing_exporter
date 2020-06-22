using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
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
            var secret_file_path = ".secrets/billing_reader_sp.json";
            using StreamReader r = new StreamReader(secret_file_path);
            string json = r.ReadToEnd();
            var settings = JsonConvert.DeserializeObject<ApiSettings>(json);

            ApiSettings = settings;
            BearerToken = GetBearerToken();
        }

        /// <summary>
        /// Authenticate to Azure vie service principals
        /// </summary>
        /// <returns>bearer token</returns>
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
        
        public string GetDailyDataYesterday()
        {
            var dateTimeNow = DateTime.Now;
            var dateStart = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day - 1);
            var dateEnd = new DateTime(dateTimeNow.Year, dateTimeNow.Month, dateTimeNow.Day - 1, 23, 59, 59);
            
            var templateQuery = File.ReadAllText("./queries/get_daily_costs.json");
            var template = Template.Parse(templateQuery); // Parses and compiles the template
            string billing_query = template.Render(Hash.FromAnonymousObject(new
            {
                DayStart = dateStart.ToString("o", CultureInfo.InvariantCulture), 
                DayEnd = dateEnd.ToString("o", CultureInfo.InvariantCulture)
            }));

            return execute_billing_query(billing_query);
        }


        private string execute_billing_query(string billing_query)
        {
            // """ Execute billing query by query text """
            // azure_management_url = f"https://management.azure.com/subscriptions%2F{self.subsription_id}/providers/Microsoft.CostManagement/query?api-version=2019-10-01"
            //
            // headers = {
            //     "content-type": "application/json",
            //     "Authorization": f"Bearer {self.bearer_token}"
            // }
            //
            // client = httplib2.Http()
            // client.follow_all_redirects = True
            //
            //     (_, content) = client.request(uri=azure_management_url,
            //     method="POST",
            //     body=query_text,
            //     headers=headers)
            //
            // billing_response_json = json.loads(content.decode('utf8'))

            string azure_management_url =
                $"https://management.azure.com/subscriptions%2F{ApiSettings.SubsriptionId}/providers/Microsoft.CostManagement/query?api-version=2019-10-01";

            using var httpClient = new HttpClient();

            var request = new HttpRequestMessage
            {
                RequestUri = new Uri(azure_management_url),
                Method = HttpMethod.Post
            };
            request.Content = new StringContent(
                billing_query,
                Encoding.UTF8,
                "application/json");
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            request.Headers.Authorization =
                new AuthenticationHeaderValue("Bearer", BearerToken);

            var response = httpClient.SendAsync(request).Result;

            var result = JsonConvert.DeserializeObject(response.Content.ReadAsStringAsync().Result);
            return "";
        }
    }
}