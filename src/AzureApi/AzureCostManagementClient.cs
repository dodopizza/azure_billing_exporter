using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using AzureBillingExporter.Cost;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace AzureBillingExporter.AzureApi
{
    public class AzureCostManagementClient
    {
        private readonly ApiSettings _apiSettings;
        private readonly IAccessTokenProvider _accessTokenProvider;
        private readonly ILogger<AzureCostManagementClient> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public AzureCostManagementClient(ApiSettings apiSettings,
            IAccessTokenProvider accessTokenProvider,
            ILogger<AzureCostManagementClient> logger,
            IHttpClientFactory httpClientFactory)
        {
            _apiSettings = apiSettings;
            _accessTokenProvider = accessTokenProvider;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public async IAsyncEnumerable<CostResultRows> ExecuteBillingQuery(string billingQuery, [EnumeratorCancellation] CancellationToken cancel)
        {
            var client = _httpClientFactory.CreateClient(nameof(AzureCostManagementClient));

            var azureManagementUrl =
                $"https://management.azure.com/subscriptions/{_apiSettings.SubscriptionId}/providers/Microsoft.CostManagement/query?api-version=2019-10-01";

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

            var response = await client.SendAsync(request, cancel);

            if (response.StatusCode != HttpStatusCode.OK)
            {
                if (response.StatusCode == HttpStatusCode.TooManyRequests)
                {
                    throw new TooManyRequestsException($"{response.StatusCode} {await response.Content.ReadAsStringAsync()}");
                }

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
