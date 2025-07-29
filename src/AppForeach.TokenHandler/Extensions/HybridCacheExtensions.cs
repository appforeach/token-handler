using Microsoft.Extensions.Caching.Hybrid;

namespace AppForeach.TokenHandler.Extensions;
public static class HybridCacheExtensions
{
    public static async ValueTask<T?> GetOrDefautAsync<T>(this HybridCache cache, string key, T? defaultValue)
    {
        if (cache == null)
            throw new ArgumentNullException(nameof(cache));

        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));

        return await cache.GetOrCreateAsync(key, factory: _ => ValueTask.FromResult(defaultValue), new HybridCacheEntryOptions { Flags = HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite });
    }
}

