using Microsoft.Extensions.Logging;

namespace AppForeach.TokenHandler.Services.Expiring_Sessions_Refresh;

internal class ExpiringTokensRefreshService(
    ITokenStorageService tokenStorageService,
    ITokenRefreshService tokenRefreshService,
    ILogger<ExpiringTokensRefreshService> logger) : IExpiringTokensRefreshService
{
    public async Task HandeAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sessionIds = await tokenStorageService.GetSessionIdsAsync(cancellationToken);

        foreach (var sessionId in sessionIds)
        {
            var tokenResponse = await tokenStorageService.GetAsync(sessionId, cancellationToken);
            if (tokenResponse is null)
            {
                //do we need to remomve sessionId from index if tokenResponse is null? probably yes, to avoid trying to refresh every time.
                await tokenStorageService.RemoveAsync(sessionId, cancellationToken);
                continue;
            }

            if (!tokenRefreshService.ShouldRefresh(tokenResponse, now))
            {
                continue;
            }

            var refreshedToken = await tokenRefreshService.RefreshAsync(tokenResponse, cancellationToken);
            if (refreshedToken is null)
            {
                logger.LogWarning("Token refresh failed for session {SessionId}.", sessionId);
                continue;
            }

            await tokenStorageService.StoreAsync(sessionId, refreshedToken, cancellationToken);
            logger.LogInformation("Refreshed token for session {SessionId}.", sessionId);
        }
    }
}
