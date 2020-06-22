using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;

namespace AzureBillingExporter
{
    public class AzureBillingMetricsGrapper
    {
        private static readonly Counter DailyCosts =
            Metrics.CreateCounter("azure_billing_daily", "Today costs for subscription");

        public async Task DownloadFromApi(CancellationToken cancel)
        {
            var restReader = new AzureRestReader();
            restReader.GetDailyDataYesterday();
            
            using var httpClient = new HttpClient();
            // Probe a remote system.
            var response = await httpClient.GetAsync("https://google.com", cancel);

            // Increase a counter by however many bytes we loaded.
            DailyCosts.Inc(response.Content.Headers.ContentLength ?? 0);
        }
    }
}