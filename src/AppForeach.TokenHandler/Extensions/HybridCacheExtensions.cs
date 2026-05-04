using Microsoft.Extensions.Caching.Hybrid;

namespace AppForeach.TokenHandler.Extensions;

public static class HybridCacheExtensions
{
    public static async ValueTask<T?> GetOrDefaultAsync<T>(this HybridCache cache, string key, T? defaultValue, CancellationToken cancellationToken = default)
    {
        if (cache == null)
            throw new ArgumentNullException(nameof(cache));

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        return await cache.GetOrCreateAsync(
            key,
            defaultValue,
            static (state, _) => ValueTask.FromResult(state),
            new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite },
            cancellationToken: cancellationToken);
    }
}

