using AppForeach.TokenHandler.Services.Expiring_Sessions_Refresh;
using Microsoft.Extensions.Options;

namespace AppForeach.TokenHandler.BackgroundWorker;

public class ExpiringTokensRefreshWorker(
    IExpiringTokensRefreshService expiringSessionsRefreshService,
    IOptions<ExpiringTokensRefreshWorkerOptions> options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (options.Value.PollingIntervalInMinutes <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ExpiringTokensRefreshWorker)}:PollingInterval must be greater than zero.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await expiringSessionsRefreshService.HandleAsync(stoppingToken);
            await Task.Delay(options.Value.PollingIntervalInMinutes, stoppingToken);
        }
    }
}
