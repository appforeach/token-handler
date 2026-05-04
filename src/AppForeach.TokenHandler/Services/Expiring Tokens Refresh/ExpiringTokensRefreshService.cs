using Microsoft.Extensions.Logging;

namespace AppForeach.TokenHandler.Services.Expiring_Sessions_Refresh;

internal class ExpiringTokensRefreshService(
    ITokenStorageService tokenStorageService,
    ITokenRefreshService tokenRefreshService,
    ILogger<ExpiringTokensRefreshService> logger) : IExpiringTokensRefreshService
{
    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var sessionIds = await tokenStorageService.GetSessionIdsAsync(cancellationToken);
        logger.LogDebug("Found {SessionCount} sessions to check for token refresh.", sessionIds.Count);
        logger.LogDebug("Session IDs to check for token refresh: {SessionIds}.", string.Join(",", sessionIds));

        foreach (var sessionId in sessionIds)
        {
            try
            {
                var tokenResponse = await tokenStorageService.GetAsync(sessionId, cancellationToken);
                if (tokenResponse is null)
                {
                    //do we need to remove sessionId from index if tokenResponse is null? probably yes, to avoid trying to refresh every time.
                    await tokenStorageService.RemoveAsync(sessionId, cancellationToken);
                    continue;
                }

                if (!tokenRefreshService.ShouldRefresh(tokenResponse, now))
                {
                    logger.LogDebug("Token for session {SessionId} does not need refresh.", sessionId);
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
            catch (Exception ex)
            {
                logger.LogError(ex, "Token refresh failed for session {SessionId}.", sessionId);
            }
        }
    }
}
