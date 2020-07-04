using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AzureBillingExporter.AzureApi
{
    public class BackgroundAccessTokenProviderHostedService : IHostedService
    {
        private readonly IAccessTokenFactory _accessTokenFactory;
        private readonly IAccessTokenProvider _accessTokenProvider;
        private readonly TimeSpan _accessTokenRefreshInterval;
        private readonly TimeSpan _failedAttemptsDelay;
        private readonly ILogger<BackgroundAccessTokenProviderHostedService> _logger;

        private readonly CancellationTokenSource _stoppingCts = new CancellationTokenSource();
        private Task _executingTask = null!;


        public BackgroundAccessTokenProviderHostedService(
            IAccessTokenProvider accessTokenProvider,
            IAccessTokenFactory accessTokenFactory,
            TimeSpan accessTokenRefreshInterval,
            ILogger<BackgroundAccessTokenProviderHostedService> logger,
            TimeSpan failedAttemptsDelay)
        {
            if (accessTokenRefreshInterval == default)
            {
                throw new ArgumentException("AccessTokenRefreshInterval can't be a zero or default");
            }

            _accessTokenProvider = accessTokenProvider ?? throw new ArgumentNullException(nameof(accessTokenProvider));
            _accessTokenFactory = accessTokenFactory ?? throw new ArgumentNullException(nameof(accessTokenFactory));
            _accessTokenRefreshInterval = accessTokenRefreshInterval;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _failedAttemptsDelay = failedAttemptsDelay;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Azure Billing API background access tokens refreshing hosted service - Starting...");
            _executingTask = StartRefreshingAccessTokensInBackgroundAsync(_stoppingCts.Token);
            if (_executingTask.IsCompleted)
            {
                return _executingTask;
            }

            _logger.LogInformation("Azure Billing API background access tokens refreshing hosted service - Has started.");
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (_executingTask == null)
            {
                _logger.LogInformation("Devices API background access tokens refreshing hosted service - Already stopped.");
                return;
            }

            try
            {
                _logger.LogInformation("Devices API background access tokens refreshing hosted service - Stopping...");
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

            _logger.LogInformation("Devices API background access tokens refreshing hosted service - Stopped.");
        }

        private async Task StartRefreshingAccessTokensInBackgroundAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var gotInitToken = false;
            while (!cancellationToken.IsCancellationRequested)
            {
                if (!gotInitToken)
                {
                    try
                    {
                        var initAccessToken = await _accessTokenFactory.CreateAsync(cancellationToken);
                        _accessTokenProvider.SetAccessToken(initAccessToken);
                        gotInitToken = true;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(new EventId(0), ex, ex.Message);
                    }

                    await Task.Delay(_failedAttemptsDelay, cancellationToken);
                }
                else
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await Task.Delay(_accessTokenRefreshInterval, cancellationToken);
                    var gotNewToken = false;
                    while (!gotNewToken)
                    {
                        try
                        {
                            var newAccessToken = await _accessTokenFactory.CreateAsync(cancellationToken);
                            _accessTokenProvider.SetAccessToken(newAccessToken);
                            gotNewToken = true;
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(new EventId(0), ex, ex.Message);
                        }

                        await Task.Delay(_failedAttemptsDelay, cancellationToken);
                    }
                }
            }
        }
    }
}