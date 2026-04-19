namespace AppForeach.TokenHandler.BackgroundWorker;

public class ExpiringTokensRefreshWorkerOptions
{
    public const string SectionName = "ExpiringTokensRefreshWorkerOptions";

    public TimeSpan PollingInterval { get; set; } = TimeSpan.FromMinutes(1);
}
