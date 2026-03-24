using Microsoft.Extensions.Caching.Memory;

namespace Poc.BackgroundWorker.Services;

public class CachedClientCredentialsTokenService : IClientCredentialsTokenService
{
    private readonly IClientCredentialsTokenService _inner;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CachedClientCredentialsTokenService> _logger;

    public CachedClientCredentialsTokenService(
        IClientCredentialsTokenService inner,
        IMemoryCache cache,
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

        if (_cache.TryGetValue<ClientCredentialsResult>(cacheKey, out var cachedResult))
        {
            _logger.LogDebug("Using cached client credentials token");
            return cachedResult!;
        }

        var result = await _inner.GetAccessTokenAsync(audience, scopes, cancellationToken);

        if (result.IsSuccess && result.ExpiresIn.HasValue)
        {
            // Cache for 80% of token lifetime to allow for clock skew
            var cacheTime = TimeSpan.FromSeconds(result.ExpiresIn.Value * 0.8);
            _cache.Set(cacheKey, result, cacheTime);
            _logger.LogDebug("Cached client credentials token for {Duration}", cacheTime);
        }

        return result;
    }
}