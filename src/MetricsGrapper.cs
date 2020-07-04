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
                        LabelNames = new [] {"UsageDate", "DateEnum"}
                    });
        
        private static readonly Gauge MonthlyCosts =
            Metrics.CreateGauge(
                "azure_billing_monthly", 
                "This month costs",
                    new GaugeConfiguration
                    {
                        LabelNames = new [] {"BillingMonth", "DateEnum"}
                    });

        private static 
            CustomCollectorConfiguration CustomCollectorConfiguration = new CustomCollectorConfiguration();

        private BillingQueryClient _billingQueryClient;
        static AzureBillingMetricsGrapper()
        {
            CustomCollectorConfiguration.ReadCustomCollectorConfig();
        }
        
        public AzureBillingMetricsGrapper(BillingQueryClient billingQueryClient)
        {
            _billingQueryClient = billingQueryClient;
        }
        
        public async Task DownloadFromApi(CancellationToken cancel)
        {
            //    Daily, monthly costs
            await foreach(var dayData in (await  _billingQueryClient.GetDailyData(cancel)).WithCancellation(cancel))
            {
                var dayEnum = DateEnumHelper.ReplaceDateValueToEnums(dayData.GetByColumnName("UsageDate"));
            
                DailyCosts
                    .WithLabels(dayData.GetByColumnName("UsageDate"),dayEnum)
                    .Set(dayData.Cost);
            }

            await foreach(var dayData in (await  _billingQueryClient.GetMonthlyData(cancel)).WithCancellation(cancel))
            {
                var monthEnum = DateEnumHelper.ReplaceDateValueToEnums(dayData.GetByColumnName("BillingMonth"));
            
                MonthlyCosts
                    .WithLabels(dayData.GetByColumnName("BillingMonth"), monthEnum)
                    .Set(dayData.Cost);
            }

            foreach (var (key, _) in CustomCollectorConfiguration.CustomGaugeMetrics)
            {
                await foreach (var customData in
                    (await _billingQueryClient.GetCustomData(cancel, key.QueryFilePath)).WithCancellation(cancel))
                {
                    CustomCollectorConfiguration.SetValues(key, customData);
                }
            }
        }
    }
}