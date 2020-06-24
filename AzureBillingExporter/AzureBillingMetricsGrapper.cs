using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;

namespace AzureBillingExporter
{
    public class AzureBillingMetricsGrapper
    {
        private static readonly Gauge DailyTodayCosts =
            Metrics.CreateGauge("azure_billing_daily_today", "Yesterday costs for subscription");
        private static readonly Gauge DailyYesterdayCosts =
            Metrics.CreateGauge("azure_billing_daily_yesterday", "Yesterday costs for subscription");
        private static readonly Gauge DailyBeforeYesterdayCosts =
            Metrics.CreateGauge("azure_billing_daily_before_yesterday", "Yesterday costs for subscription");
        
        
        private static readonly Gauge MonthlyCosts =
            Metrics.CreateGauge("azure_billing_monthly", "This month costs");

        private static 
            CustomCollectorConfiguration CustomCollectorConfiguration = new CustomCollectorConfiguration();

        private AzureRestReader AzureRestApiClient;
        static AzureBillingMetricsGrapper()
        {
            CustomCollectorConfiguration.ReadCustomCollectorConfig();
        }
        
        public AzureBillingMetricsGrapper(AzureRestReader azureRestReader)
        {
            AzureRestApiClient = azureRestReader;
        }
        
        public async Task DownloadFromApi(CancellationToken cancel)
        {
            //    Daily, monthly costs
            var dailyCosts = await  AzureRestApiClient.GetDailyData(cancel);
            var monthlyCosts = await  AzureRestApiClient.GetMonthlyData(cancel);

            await foreach(var dayData in dailyCosts.WithCancellation(cancel))
            {
                if (dayData.Date == DateTime.Now.ToString("yyyyMMdd"))
                {
                    DailyTodayCosts.Set(dayData.Cost);
                }
                if (dayData.Date == DateTime.Now.AddDays(-1).ToString("yyyyMMdd"))
                {
                    DailyYesterdayCosts.Set(dayData.Cost);
                }
                if (dayData.Date == DateTime.Now.AddDays(-2).ToString("yyyyMMdd"))
                {
                    DailyBeforeYesterdayCosts.Set(dayData.Cost);
                }
            }

            MonthlyCosts.Set(monthlyCosts.Cost);

            foreach (var (key, value) in CustomCollectorConfiguration.CustomGaugeMetrics)
            {
                await foreach (var customData in
                    (await AzureRestApiClient.GetCustomData(cancel, key.QueryFilePath)).WithCancellation(cancel))
                {
                    CustomCollectorConfiguration.SetValues(key, customData);
                }
            }
        }
    }
}