using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        private static readonly IEnumerable<Gauge> CustomMetrics;
        static AzureBillingMetricsGrapper()
        {
            CustomMetrics = new List<Gauge>();
        }
        
        public async Task DownloadFromApi(CancellationToken cancel)
        {
            var restReader = new AzureRestReader();
            var dailyCosts = await  restReader.GetDailyData(cancel);
            var monthlyCosts = await  restReader.GetMonthlyData(cancel);

            await foreach(var dayData in dailyCosts)
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
        }
    }
}