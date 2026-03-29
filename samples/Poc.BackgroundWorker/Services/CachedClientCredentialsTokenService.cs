using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace Poc.BackgroundWorker.Services;

public class CachedClientCredentialsTokenService : IClientCredentialsTokenService
{
    private readonly IClientCredentialsTokenService _inner;
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachedClientCredentialsTokenService> _logger;

    public CachedClientCredentialsTokenService(
        IClientCredentialsTokenService inner,
        IDistributedCache cache,
        ILogger<CachedClientCredentialsTokenService> logger)
    {
        _inner = inner;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ClientCredentialsResult> GetAccessTokenAsync(
        string? audience = null,
        IEnumerable<string>? scopes = null,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = $"client_credentials:{audience ?? "default"}:{string.Join(",", scopes ?? [])}";

        var cachedBytes = await _cache.GetAsync(cacheKey, cancellationToken);
        if (cachedBytes is not null)
        {
            _logger.LogDebug("Using cached client credentials token");
            return JsonSerializer.Deserialize<ClientCredentialsResult>(cachedBytes)!;
        }

        var result = await _inner.GetAccessTokenAsync(audience, scopes, cancellationToken);

        if (result.IsSuccess && result.ExpiresIn.HasValue)
        {
            // Cache for 80% of token lifetime to allow for clock skew
            var cacheTime = TimeSpan.FromSeconds(result.ExpiresIn.Value * 0.8);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheTime
            };
            await _cache.SetAsync(cacheKey, JsonSerializer.SerializeToUtf8Bytes(result), options, cancellationToken);
            _logger.LogDebug("Cached client credentials token for {Duration}", cacheTime);
        }

        return result;
    }
}