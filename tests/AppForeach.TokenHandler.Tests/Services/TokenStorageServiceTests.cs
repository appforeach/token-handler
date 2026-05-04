using AppForeach.TokenHandler.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AppForeach.TokenHandler.Tests.Services;

public class TokenStorageServiceTests
{
    [Fact]
    public async Task StoreAsync_AddsSessionToCacheAndIndex()
    {
        var cache = new TestHybridCache();
        var distributedCache = new TestDistributedCache();
        var store = new TokenStorageService(cache, distributedCache);
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = "access-token",
            RefreshToken = "refresh-token"
        };

        await store.StoreAsync("session-1", tokenResponse);

        var storedToken = await store.GetAsync("session-1");
        var sessionIds = await store.GetSessionIdsAsync();

        Assert.NotNull(storedToken);
        Assert.Equal("access-token", storedToken.AccessToken);
        Assert.Contains("session-1", sessionIds);
    }

    [Fact]
    public async Task RemoveAsync_RemovesSessionFromCacheAndIndex()
    {
        var cache = new TestHybridCache();
        var distributedCache = new TestDistributedCache();
        var store = new TokenStorageService(cache, distributedCache);
        await store.StoreAsync("session-1", new OpenIdConnectMessage { AccessToken = "access-token" });

        await store.RemoveAsync("session-1");

        var storedToken = await store.GetAsync("session-1");
        var sessionIds = await store.GetSessionIdsAsync();

        Assert.Null(storedToken);
        Assert.DoesNotContain("session-1", sessionIds);
    }

    private sealed class TestDistributedCache : IDistributedCache
    {
        private readonly Dictionary<string, byte[]> _values = new(StringComparer.Ordinal);

        public byte[]? Get(string key) => _values.TryGetValue(key, out var value) ? value : null;

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default) =>
            Task.FromResult(Get(key));

        public void Refresh(string key)
        {
        }

        public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;

        public void Remove(string key) => _values.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default)
        {
            Remove(key);
            return Task.CompletedTask;
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
            _values[key] = value;

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
        {
            Set(key, value, options);
            return Task.CompletedTask;
        }
    }

    private sealed class TestHybridCache : HybridCache
    {
        private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (_values.TryGetValue(key, out var value))
            {
                return ValueTask.FromResult((T)value);
            }

            return factory(state, cancellationToken);
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _values.Remove(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            _values[key] = value!;
            return ValueTask.CompletedTask;
        }
    }
}
