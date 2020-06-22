using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;

namespace AzureBillingExporter
{
    public class AzureBillingMetricsGrapper
    {
        private static readonly Gauge YesterdayDailyCosts =
            Metrics.CreateGauge("azure_billing_daily_yesterday", "Yesterday costs for subscription");

        public async Task DownloadFromApi(CancellationToken cancel)
        {
            var restReader = new AzureRestReader();
            var dailyCosts = restReader.GetDailyDataYesterday();
            
            // Increase a counter by however many bytes we loaded.
            YesterdayDailyCosts.Set(dailyCosts.ElementAt(0).Cost);
        }
    }
}