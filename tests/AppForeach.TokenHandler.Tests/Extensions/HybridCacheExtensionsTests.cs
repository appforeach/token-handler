using AppForeach.TokenHandler.Extensions;
using Microsoft.Extensions.Caching.Hybrid;

namespace AppForeach.TokenHandler.Tests.Extensions;

public class HybridCacheExtensionsTests
{
    [Fact]
    public async Task GetOrDefaultAsync_NullCache_ThrowsArgumentNullException()
    {
        // Arrange
        HybridCache? cache = null;
        var key = "test-key";
        var defaultValue = "default";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await cache!.GetOrDefaultAsync(key, defaultValue));
    }

    [Fact]
    public async Task GetOrDefaultAsync_NullKey_ThrowsArgumentException()
    {
        // Arrange
        var cache = new TestHybridCache();
        string? key = null;
        var defaultValue = "default";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await cache.GetOrDefaultAsync(key!, defaultValue));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public async Task GetOrDefaultAsync_EmptyKey_ThrowsArgumentException()
    {
        // Arrange
        var cache = new TestHybridCache();
        var key = string.Empty;
        var defaultValue = "default";

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(async () =>
            await cache.GetOrDefaultAsync(key, defaultValue));
        Assert.Equal("key", exception.ParamName);
    }

    [Fact]
    public async Task GetOrDefaultAsync_ValidParameters_CallsGetOrCreateAsync()
    {
        // Arrange
        var cache = new TestHybridCache();
        var key = "test-key";
        var defaultValue = "test-value";

        // Act
        var result = await cache.GetOrDefaultAsync(key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
        Assert.True(cache.GetOrCreateAsyncCalled);
        Assert.Equal(key, cache.LastKey);
        Assert.NotNull(cache.LastOptions);
        Assert.Equal(HybridCacheEntryFlags.DisableLocalCacheWrite | HybridCacheEntryFlags.DisableDistributedCacheWrite, cache.LastOptions!.Flags);
    }

    [Fact]
    public async Task GetOrDefaultAsync_WithIntValue_ReturnsDefaultValue()
    {
        // Arrange
        var cache = new TestHybridCache();
        var key = "test-key";
        var defaultValue = 42;

        // Act
        var result = await cache.GetOrDefaultAsync(key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, result);
    }

    [Fact]
    public async Task GetOrDefaultAsync_NullDefaultValue_ReturnsNull()
    {
        // Arrange
        var cache = new TestHybridCache();
        var key = "test-key";
        string? defaultValue = null;

        // Act
        var result = await cache.GetOrDefaultAsync(key, defaultValue);

        // Assert
        Assert.Null(result);
    }

    private class TestHybridCache : HybridCache
    {
        public bool GetOrCreateAsyncCalled { get; private set; }
        public string? LastKey { get; private set; }
        public HybridCacheEntryOptions? LastOptions { get; private set; }

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            GetOrCreateAsyncCalled = true;
            LastKey = key;
            LastOptions = options;
            return factory(state, cancellationToken);
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
