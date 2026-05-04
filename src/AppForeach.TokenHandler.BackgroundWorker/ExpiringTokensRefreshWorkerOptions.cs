namespace AppForeach.TokenHandler.BackgroundWorker;

public class ExpiringTokensRefreshWorkerOptions
{
    public const string SectionName = "ExpiringTokensRefreshWorker";

    public TimeSpan PollingIntervalInMinutes { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan RefreshBeforeExpirationInMinutes { get; set; } = TimeSpan.FromMinutes(5);
}
