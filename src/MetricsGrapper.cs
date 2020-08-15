using System.Threading;
using System.Threading.Tasks;
using Prometheus;

namespace AzureBillingExporter
{
    public class AzureBillingMetricsGrapper
    {
        private static readonly Gauge DailyCosts =
            Metrics.CreateGauge(
                "azure_billing_daily",
                "Daily cost by today, yesterday and day before yesterday",
                    new GaugeConfiguration
                    {
                        LabelNames = new [] {"DateEnum"}
                    });

        private static readonly Gauge MonthlyCosts =
            Metrics.CreateGauge(
                "azure_billing_monthly",
                "This month costs",
                    new GaugeConfiguration
                    {
                        LabelNames = new [] {"DateEnum"}
                    });

        private BillingQueryClient _billingQueryClient;
        private CustomCollectorConfiguration _customCollectorConfiguration;

        public AzureBillingMetricsGrapper(BillingQueryClient billingQueryClient, CustomCollectorConfiguration customCollectorConfiguration)
        {
            _billingQueryClient = billingQueryClient;
            _customCollectorConfiguration = customCollectorConfiguration;
            _customCollectorConfiguration.ReadCustomCollectorConfig();
        }

        public async Task DownloadFromApi(CancellationToken cancel)
        {
            //    Daily, monthly costs
            await foreach(var dayData in (await  _billingQueryClient.GetDailyData(cancel)).WithCancellation(cancel))
            {
                var dayEnum = DateEnumHelper.ReplaceDateValueToEnums(dayData.GetByColumnName("UsageDate"));

                DailyCosts
                    .WithLabels(dayEnum)
                    .Set(dayData.Cost);
            }

            await foreach(var dayData in (await  _billingQueryClient.GetMonthlyData(cancel)).WithCancellation(cancel))
            {
                var monthEnum = DateEnumHelper.ReplaceDateValueToEnums(dayData.GetByColumnName("BillingMonth"));

                MonthlyCosts
                    .WithLabels(monthEnum)
                    .Set(dayData.Cost);
            }

            foreach (var (key, _) in _customCollectorConfiguration.CustomGaugeMetrics)
            {
                await foreach (var customData in
                    (await _billingQueryClient.GetCustomData(cancel, key.QueryFilePath)).WithCancellation(cancel))
                {
                    _customCollectorConfiguration.SetValues(key, customData);
                }
            }
        }
    }
}
