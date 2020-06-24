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

        private static readonly Dictionary<string, Gauge> CustomMetrics = new Dictionary<string, Gauge>();    // <filename, Gauge>
        static AzureBillingMetricsGrapper()
        {
            var customQueriesFiles = Directory.GetFiles("custom_queries");
            foreach (var fileName in customQueriesFiles)
            {
                var metricName = Path.GetFileNameWithoutExtension(fileName);
                CustomMetrics.Add(fileName, 
                    Metrics.CreateGauge(metricName, $"Custom metrics from {metricName}",
                                    new GaugeConfiguration()
                                    {
                                        LabelNames = new[]
                                        {
                                            //"ResourceLocation", 
                                            "ResourceGroupName"
                                        }
                                    }));
            }
        }
        
        public async Task DownloadFromApi(CancellationToken cancel)
        {
            var restReader = new AzureRestReader();
            
            //    Daily, monthly costs
            var dailyCosts = await  restReader.GetDailyData(cancel);
            var monthlyCosts = await  restReader.GetMonthlyData(cancel);

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
            
            // Custom metrics
            foreach (var (metricKey, gauge) in CustomMetrics)
            {
                await foreach (var customData in (await restReader.GetCustomData(cancel, metricKey)).WithCancellation(cancel))
                {
                    if (!string.IsNullOrEmpty(customData.GetByColumnName("ResourceLocation")))
                    {
                        // gauge
                        //     .WithLabels(customData.GetByColumnName("ResourceLocation"))
                        //     .Set(customData.Cost);
                    }
                    else
                    {
                        gauge
                            .WithLabels(customData.GetByColumnName("ResourceGroupName"))
                            .Set(customData.Cost);
                        
                    }
                }
            }
        }
    }
}