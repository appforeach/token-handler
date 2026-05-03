namespace AppForeach.TokenHandler.Services.Expiring_Sessions_Refresh;

public interface IExpiringTokensRefreshService
{
    Task HandleAsync(CancellationToken cancellationToken);
}