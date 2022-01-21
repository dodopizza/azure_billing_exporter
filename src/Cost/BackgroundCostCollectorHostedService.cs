using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AzureBillingExporter.AzureApi;
using AzureBillingExporter.Configuration;
using AzureBillingExporter.Configuration.Metrics;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureBillingExporter.Cost
{
    public class BackgroundCostCollectorHostedService : IHostedService
    {
        const string DailyMetricKey = "daily";
        const string MonthlyMetricKey = "monthly";

        const int MaxOldestTimeDriftInMinutes = 10;
        const int ThrottleAzureApiTimeInMinutes = 1;

        private readonly Dictionary<string, DateTime> _metricsStats = new Dictionary<string, DateTime>();
        private readonly BillingQueryClient _billingQueryClient;
        private readonly CostDataCache _costDataCache;
        private readonly CustomCollectorConfiguration _customCollectorConfiguration;
        private readonly EnvironmentConfiguration _environmentConfiguration;
        private readonly ILogger<BackgroundCostCollectorHostedService> _logger;

        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private Task _executingTask = null!;
        private CustomCollectorConfig _customCollectorConfig;

        public BackgroundCostCollectorHostedService(
            BillingQueryClient billingQueryClient,
            CostDataCache costDataCache,
            CustomCollectorConfiguration customCollectorConfiguration,
            EnvironmentConfiguration environmentConfiguration,
            ILogger<BackgroundCostCollectorHostedService> logger)
        {
            _billingQueryClient = billingQueryClient ?? throw new ArgumentNullException(nameof(billingQueryClient));
            _costDataCache = costDataCache ?? throw new ArgumentNullException(nameof(costDataCache));
            _customCollectorConfiguration = customCollectorConfiguration ?? throw new ArgumentNullException(nameof(customCollectorConfiguration));
            _environmentConfiguration = environmentConfiguration ?? throw new ArgumentNullException(nameof(environmentConfiguration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Azure Billing data collector background hosted service - Starting...");

            FillMetricsStats();

            _logger.LogInformation($"Total metrics to process: {_metricsStats.Count}");

            _executingTask = StartCollectingDataInBackgroundAsync(_stoppingCts.Token);
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            _logger.LogInformation("Azure Billing data collector background hosted service - Has started.");
            return Task.CompletedTask;
        }

        private void FillMetricsStats()
        {
            var notActualTime = DateTime.UtcNow.AddMinutes(-1*_environmentConfiguration.CollectPeriodInMinutes);
            _metricsStats.Add(DailyMetricKey, notActualTime);
            _metricsStats.Add(MonthlyMetricKey, notActualTime);

            _customCollectorConfig = _customCollectorConfiguration.GetCustomCollectorConfig();
            foreach (var metric in _customCollectorConfig.Metrics)
            {
                _metricsStats.Add(metric.QueryFilePath, notActualTime);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                _logger.LogInformation("Azure Billing data collector background hosted service - Already stopped.");
                return;
            }

            try
            {
                _logger.LogInformation("Azure Billing data collector background hosted service - Stopping...");
                // Signal cancellation to the executing method
                _stoppingCts.Cancel();
            }
            finally
            {
                // Wait until the task completes or the stop token triggers
                await Task.WhenAny(
                    _executingTask,
                    Task.Delay(Timeout.Infinite, cancellationToken));
            }

            _logger.LogInformation("Azure Billing data collector background hosted service - Stopped.");
        }

        private async Task StartCollectingDataInBackgroundAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var timer = Stopwatch.StartNew();
            while (!cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var actualDateTimeUtc = DateTime.UtcNow.AddMinutes(-1*_environmentConfiguration.CollectPeriodInMinutes);
                if (_metricsStats.All(x => x.Value > actualDateTimeUtc))
                {
                    var sleepTimeInMinutes =
                        Math.Floor(_environmentConfiguration.CollectPeriodInMinutes - timer.Elapsed.TotalMinutes);
                    _logger.Log(LogLevel.Debug,
                        $"Will sleep next {sleepTimeInMinutes} minute(s)");
                    if (sleepTimeInMinutes > 0)
                    {
                        await Task.Delay(TimeSpan.FromMinutes(sleepTimeInMinutes), cancellationToken);
                    }
                    timer.Restart();
                }

                var oldestMetric = _metricsStats.OrderBy(x => x.Value).First();

                WarnOnTooOldMetric(oldestMetric);

                try
                {
                    await UpdateMetricData(oldestMetric.Key, cancellationToken);
                }
                catch (TooManyRequestsException ex)
                {
                    _logger.LogError(ex, ex.Message);
                    _logger.LogWarning($"Too many requests to Azure Billing API. Let's sleep for {ThrottleAzureApiTimeInMinutes} minute(s)");
                    await Task.Delay(TimeSpan.FromMinutes(ThrottleAzureApiTimeInMinutes), cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, ex.Message);
                }
                finally
                {
                    _metricsStats[oldestMetric.Key] = DateTime.UtcNow;
                }
            }
        }

        private void WarnOnTooOldMetric(KeyValuePair<string, DateTime> metric)
        {
            var maxOldestTimeInMinutes = _environmentConfiguration.CollectPeriodInMinutes + MaxOldestTimeDriftInMinutes;
            if (metric.Value <= DateTime.UtcNow.AddMinutes(-1 * maxOldestTimeInMinutes))
            {
                _logger.Log(LogLevel.Warning,
                    $"Metric {metric.Key} last update time was more than {maxOldestTimeInMinutes} minutes ago ({metric.Value})");
            }
        }

        private async Task UpdateMetricData(string metricKey, CancellationToken cancellationToken)
        {
            switch (metricKey)
            {
                case DailyMetricKey:
                {
                    _logger.Log(LogLevel.Debug, "Get daily metric data");
                    var dailyData = new List<CostResultRows>();
                    await foreach (var data in (await _billingQueryClient.GetDailyData(cancellationToken))
                        .WithCancellation(cancellationToken))
                    {
                        dailyData.Add(data);
                    }

                    _costDataCache.SetDailyCost(dailyData);

                    break;
                }
                case MonthlyMetricKey:
                {
                    _logger.Log(LogLevel.Debug, "Get monthly metric data");
                    var monthlyData = new List<CostResultRows>();
                    await foreach (var data in (await _billingQueryClient.GetMonthlyData(cancellationToken))
                        .WithCancellation(cancellationToken))
                    {
                        monthlyData.Add(data);
                    }

                    _costDataCache.SetMonthlyCost(monthlyData);
                    break;
                }
                default:
                {
                    _logger.Log(LogLevel.Debug, $"Get custom metric {metricKey} data");

                    var metricConfig =
                        _customCollectorConfig.Metrics.Single(x => x.QueryFilePath == metricKey);

                    var metricsData = new List<CostResultRows>();
                    var data = await _billingQueryClient.GetCustomData(cancellationToken,
                        metricConfig.QueryFilePath);
                    await foreach (var customData in data.WithCancellation(cancellationToken))
                    {
                        metricsData.Add(customData);
                    }

                    _costDataCache.SetCostByKey(metricKey, metricsData);
                    break;
                }
            }
        }
    }
}
