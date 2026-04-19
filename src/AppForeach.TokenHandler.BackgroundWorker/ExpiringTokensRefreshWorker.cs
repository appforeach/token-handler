using AppForeach.TokenHandler.Services.Expiring_Sessions_Refresh;
using Microsoft.Extensions.Options;

namespace AppForeach.TokenHandler.BackgroundWorker;

public class ExpiringTokensRefreshWorker : BackgroundService
{
    private readonly ExpiringTokensRefreshWorkerOptions _options;
    private readonly ILogger<ExpiringTokensRefreshWorker> _logger;
    private readonly IExpiringTokensRefreshService _expiringSessionsRefreshService;

    public ExpiringTokensRefreshWorker(
        IExpiringTokensRefreshService expiringSessionsRefreshService,
        IOptions<ExpiringTokensRefreshWorkerOptions> options,
        ILogger<ExpiringTokensRefreshWorker> logger)
    {
        _expiringSessionsRefreshService = expiringSessionsRefreshService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.PollingInterval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException($"{nameof(ExpiringTokensRefreshWorker)}:PollingInterval must be greater than zero.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            await _expiringSessionsRefreshService.HandeAsync(stoppingToken);
            await Task.Delay(_options.PollingInterval, stoppingToken);
        }
    }
}
