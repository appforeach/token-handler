using AppForeach.TokenHandler.Services;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace AppForeach.TokenHandler.Tests.Services;

public class TokenStorageServiceTests
{
    [Fact]
    public async Task StoreAsync_AddsSessionToCacheAndIndex()
    {
        var cache = new TestHybridCache();
        var indexStore = new InMemorySessionIndexStore();
        var store = new TokenStorageService(cache, indexStore);
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
        var indexStore = new InMemorySessionIndexStore();
        var store = new TokenStorageService(cache, indexStore);
        await store.StoreAsync("session-1", new OpenIdConnectMessage { AccessToken = "access-token" });

        await store.RemoveAsync("session-1");

        var storedToken = await store.GetAsync("session-1");
        var sessionIds = await store.GetSessionIdsAsync();

        Assert.Null(storedToken);
        Assert.DoesNotContain("session-1", sessionIds);
    }

    private sealed class InMemorySessionIndexStore : ISessionIndexStore
    {
        private readonly HashSet<string> _sessionIds = new(StringComparer.Ordinal);

        public Task AddAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            _sessionIds.Add(sessionId);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string sessionId, CancellationToken cancellationToken = default)
        {
            _sessionIds.Remove(sessionId);
            return Task.CompletedTask;
        }

        public Task<IReadOnlyCollection<string>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<string>>(_sessionIds.ToHashSet(StringComparer.Ordinal));
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
