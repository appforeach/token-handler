namespace AppForeach.TokenHandler.BackgroundWorker;

public class ExpiringTokensRefreshWorkerOptions
{
    public const string SectionName = "ExpiringTokensRefreshWorker";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(1);
    public TimeSpan RefreshBeforeExpiration { get; set; } = TimeSpan.FromMinutes(5);
}
