using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using AzureBillingExporter.Cost;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureBillingExporter.AzureApi
{
    public class AccessTokenFactory: IAccessTokenFactory
    {
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<BillingQueryClient> _logger;

        public AccessTokenFactory(
            ApiSettings apiSettings, 
            ILogger<BillingQueryClient> logger)
        {
            _apiSettings = apiSettings;
            _logger = logger;
        }
        public async Task<string> CreateAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Into GetBearerToken");
            var azureAdUrl = $"https://login.microsoftonline.com/{_apiSettings.TenantId}/oauth2/token";

            var resourceEncoded = HttpUtility.UrlEncode("https://management.azure.com");
            var clientSecretEncoded = HttpUtility.UrlEncode(_apiSettings.ClientSecret);
            var bodyStr =
                $"grant_type=client_credentials&client_id={_apiSettings.ClientId}&client_secret={clientSecretEncoded}&resource={resourceEncoded}";

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
                    "application/x-www-form-urlencoded"), cancellationToken).Result;

            var responseContent = await response.Content.ReadAsStringAsync();
            _logger.LogTrace($"Bearer response content {responseContent}");
            var result = JsonConvert.DeserializeObject<AzureAdResult>(responseContent); 

            return result.access_token;
        }
    }
}