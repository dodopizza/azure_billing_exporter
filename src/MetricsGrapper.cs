using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Prometheus;

namespace AzureBillingExporter
{
    public class AzureBillingMetricsGrapper
    {
        private readonly ILogger<AzureBillingMetricsGrapper> _logger;

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
        private CostDataCache _сostDataCache;

        public AzureBillingMetricsGrapper(BillingQueryClient billingQueryClient,
            CustomCollectorConfiguration customCollectorConfiguration,
            CostDataCache сostDataCache,
            ILogger<AzureBillingMetricsGrapper> logger)
        {
            _billingQueryClient = billingQueryClient;
            _customCollectorConfiguration = customCollectorConfiguration;
            _сostDataCache = сostDataCache;
            _logger = logger;
            _customCollectorConfiguration.ReadCustomCollectorConfig();
        }

        public async Task DownloadFromApi(CancellationToken cancel)
        {
            _logger.Log(LogLevel.Information, "Start getting data for metrics");
            var timer = new Stopwatch();
            timer.Start();

            //    Daily, monthly costs
            _logger.Log(LogLevel.Debug, "Get daily data");
            foreach(var data in _сostDataCache.GetDailyCost())
            {
                var dayEnum = DateEnumHelper.ReplaceDateValueToEnums(data.GetByColumnName("UsageDate"));

                DailyCosts
                    .WithLabels(dayEnum)
                    .Set(data.Cost);
            }

            _logger.Log(LogLevel.Debug, "Get monthly data");
            foreach(var data in _сostDataCache.GetMonthlyCost())
            {
                var monthEnum = DateEnumHelper.ReplaceDateValueToEnums(data.GetByColumnName("BillingMonth"));

                MonthlyCosts
                    .WithLabels(monthEnum)
                    .Set(data.Cost);
            }

            _logger.Log(LogLevel.Debug, "Get custom data in parallel");
            // var customMetricsDataTasks = _customCollectorConfiguration.CustomGaugeMetrics.Keys
            //     .Select(x => StartMetricDataTask(x, cancel)).ToArray();
            //
            // Task.WaitAll(customMetricsDataTasks, cancel);
            // foreach (var task in customMetricsDataTasks)
            // {
            //     if (task != null && task.Exception != null)
            //         throw task.Exception;
            // }
            //
            // foreach (var task in customMetricsDataTasks)
            // {
            //     await foreach (var customData in task.Result.Data.WithCancellation(cancel))
            //     {
            //         _customCollectorConfiguration.SetValues(task.Result.Config, customData);
            //     }
            // }

            timer.Stop();
            _logger.Log(LogLevel.Debug, "Metrics get total time: " + timer.Elapsed);

            _logger.Log(LogLevel.Information, "Finish getting data for metrics");
        }

        private async Task<MetricsData> StartMetricDataTask(MetricConfig config, CancellationToken cancel)
        {
            _logger.Log(LogLevel.Debug, $"Get custom data for {config.MetricName} metric, query file path - {config.QueryFilePath}");
            var data = await _billingQueryClient.GetCustomData(cancel, config.QueryFilePath);
            return new MetricsData
            {
                Config = config,
                Data = data
            };
        }
    }

    class MetricsData
    {
        public MetricConfig Config { get; set; }
        public IAsyncEnumerable<CostResultRows> Data { get; set; }
    }
}
