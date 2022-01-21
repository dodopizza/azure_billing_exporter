using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureBillingExporter
{
    public class BackgroundCostCollectorHostedService : IHostedService
    {
        private readonly BillingQueryClient _billingQueryClient;
        private readonly CostDataCache _costDataCache;
        private readonly ILogger<BackgroundCostCollectorHostedService> _logger;

        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private Task _executingTask = null!;


        public BackgroundCostCollectorHostedService(
            BillingQueryClient billingQueryClient,
            CostDataCache costDataCache,
            ILogger<BackgroundCostCollectorHostedService> logger)
        {
            _billingQueryClient = billingQueryClient ?? throw new ArgumentNullException(nameof(billingQueryClient));
            _costDataCache = costDataCache ?? throw new ArgumentNullException(nameof(costDataCache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Azure Billing data collector background hosted service - Starting...");
            _executingTask = StartCollectingDataInBackgroundAsync(_stoppingCts.Token);
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            _logger.LogInformation("Azure Billing data collector background hosted service - Has started.");
            return Task.CompletedTask;
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
            while (!cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    //    Daily, monthly costs
                    _logger.Log(LogLevel.Debug, "Get daily data");
                    var dailyData = new List<CostResultRows>();
                    await foreach(var data in (await  _billingQueryClient.GetDailyData(cancellationToken)).WithCancellation(cancellationToken))
                    {
                        dailyData.Add(data);
                    }
                    _costDataCache.SetDailyCost(dailyData);

                    _logger.Log(LogLevel.Debug, "Get monthly data");
                    var monthlyData = new List<CostResultRows>();
                    await foreach(var data in (await  _billingQueryClient.GetMonthlyData(cancellationToken)).WithCancellation(cancellationToken))
                    {
                        monthlyData.Add(data);
                    }
                    _costDataCache.SetMonthlyCost(monthlyData);
                }
                catch (Exception ex)
                {
                    _logger.LogError(new EventId(0), ex, ex.Message);
                }

                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken);
            }
        }
    }
}
