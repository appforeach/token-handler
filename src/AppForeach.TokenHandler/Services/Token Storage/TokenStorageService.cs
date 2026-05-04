using AppForeach.TokenHandler.Extensions;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Text.Json;

namespace AppForeach.TokenHandler.Services;

public class TokenStorageService : ITokenStorageService
{
    internal const string SessionIndexCacheKey = "token-handler:sessions";

    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HybridCache _cache;
    private readonly IDistributedCache _distributedCache;
    private readonly SemaphoreSlim _sessionIndexLock = new(1, 1);

    public TokenStorageService(HybridCache cache, IDistributedCache distributedCache)
    {
        _cache = cache;
        _distributedCache = distributedCache;
    }

    public async Task StoreAsync(string sessionId, OpenIdConnectMessage tokenResponse, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(tokenResponse);

        await _cache.SetAsync(sessionId, tokenResponse, cancellationToken: cancellationToken);
        await UpdateSessionIndexAsync(sessionId, action: ActionWithSession.Add, cancellationToken);
    }

    public ValueTask<OpenIdConnectMessage?> GetAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        return _cache.GetOrDefaultAsync<OpenIdConnectMessage>(sessionId, default, cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetSessionIdsAsync(CancellationToken cancellationToken = default)
    {
        var sessionIds = await ReadSessionIdsAsync(cancellationToken);
        return sessionIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
    }

    public async Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        await _cache.RemoveAsync(sessionId, cancellationToken);
        await UpdateSessionIndexAsync(sessionId, action: ActionWithSession.Remove, cancellationToken);
    }

    private async Task UpdateSessionIndexAsync(string sessionId, ActionWithSession action, CancellationToken cancellationToken)
    {
        // Implement Distributed Lock when necessary for scaling out purposes.

        await _sessionIndexLock.WaitAsync(cancellationToken);

        try
        {
            var sessionIds = await ReadSessionIdsAsync(cancellationToken);

            if (action == ActionWithSession.Add)
            {
                sessionIds.Add(sessionId);
            }
            else if (action == ActionWithSession.Remove)
            {
                sessionIds.Remove(sessionId);
            }

            var payload = JsonSerializer.SerializeToUtf8Bytes(sessionIds, SerializerOptions);
            await _distributedCache.SetAsync(SessionIndexCacheKey, payload, new DistributedCacheEntryOptions(), cancellationToken);
        }
        finally
        {
            _sessionIndexLock.Release();
        }
    }

    private async Task<HashSet<string>> ReadSessionIdsAsync(CancellationToken cancellationToken)
    {
        var payload = await _distributedCache.GetAsync(SessionIndexCacheKey, cancellationToken);
        if (payload is null || payload.Length == 0)
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return JsonSerializer.Deserialize<HashSet<string>>(payload, SerializerOptions)
            ?? new HashSet<string>(StringComparer.Ordinal);
    }

    enum ActionWithSession
    {
        Add,
        Remove
    }
}
