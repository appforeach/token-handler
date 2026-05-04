using AppForeach.TokenHandler.Extensions;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AppForeach.TokenHandler.Services;

public class TokenStorageService : ITokenStorageService
{
    private readonly HybridCache _cache;
    private readonly ISessionIndexStore _sessionIndexStore;

    public TokenStorageService(HybridCache cache, ISessionIndexStore sessionIndexStore)
    {
        _cache = cache;
        _sessionIndexStore = sessionIndexStore;
    }

    public async Task StoreAsync(string sessionId, OpenIdConnectMessage tokenResponse, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(tokenResponse);

        await _cache.SetAsync(sessionId, tokenResponse, cancellationToken: cancellationToken);
        await _sessionIndexStore.AddAsync(sessionId, cancellationToken);
    }

    public ValueTask<OpenIdConnectMessage?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _cache.GetOrDefaultAsync<OpenIdConnectMessage>(sessionId, default, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetSessionIdsAsync(CancellationToken cancellationToken = default)
    {
        var sessionIds = await _sessionIndexStore.GetAllAsync(cancellationToken);
        return sessionIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    public async Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await _cache.RemoveAsync(sessionId, cancellationToken);
        await _sessionIndexStore.RemoveAsync(sessionId, cancellationToken);
    }
}
