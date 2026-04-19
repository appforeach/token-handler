namespace AppForeach.TokenHandler.Services.Expiring_Sessions_Refresh;

public interface IExpiringTokensRefreshService
{
    Task HandeAsync(CancellationToken cancellationToken);
}