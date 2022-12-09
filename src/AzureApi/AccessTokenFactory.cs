using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using AzureBillingExporter.Cost;
using Microsoft.Extensions.Logging;

namespace AzureBillingExporter.AzureApi
{
    public class AccessTokenFactory : IAccessTokenFactory
    {
        private readonly ApiSettings _apiSettings;
        private readonly ILogger<BillingQueryClient> _logger;
        private readonly TokenCredential credentials;

        public AccessTokenFactory(
            ApiSettings apiSettings,
            ILogger<BillingQueryClient> logger)
        {
            _apiSettings = apiSettings;
            _logger = logger;
            
            if (!string.IsNullOrEmpty(_apiSettings.ClientId))
            {
                _logger.LogTrace($"Using client credentials for Client ID ", _apiSettings.ClientId);
                credentials = new ClientSecretCredential(_apiSettings.TenantId, _apiSettings.ClientId, _apiSettings.ClientSecret);
            }
            else
            {
                _logger.LogTrace($"Using default Azure credentials for Teanant ID", _apiSettings.TenantId);

                var credentialOptions = new DefaultAzureCredentialOptions()
                {
                    TenantId = _apiSettings.TenantId,
                    ExcludeInteractiveBrowserCredential = true
                };
                credentials = new DefaultAzureCredential(credentialOptions);
            }
        }

        public async Task<string> CreateAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Into GetBearerToken");

            var token = await credentials.GetTokenAsync(new TokenRequestContext(new[] { "https://management.azure.com/.default" }), cancellationToken);
            _logger.LogTrace("Got token {token}", token.Token);

            return token.Token;
        }
    }
}
