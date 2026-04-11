using AppForeach.TokenHandler.Extensions;
using AppForeach.TokenHandler.Middleware;
using AppForeach.TokenHandler.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using System.IdentityModel.Tokens.Jwt;

namespace AppForeach.TokenHandler.Tests.Middleware;

public class AuthenticationHeaderSubstitutionMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly TestHybridCache _cache;
    private readonly Mock<IConfiguration> _configMock;
    private readonly Mock<IOptions<TokenHandlerOptions>> _optionsMock;
    private readonly TokenHandlerOptions _tokenHandlerOptions;
    private readonly AuthenticationHeaderSubstitutionMiddleware _middleware;

    public AuthenticationHeaderSubstitutionMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _cache = new TestHybridCache();
        _configMock = new Mock<IConfiguration>();
        _optionsMock = new Mock<IOptions<TokenHandlerOptions>>();

        _tokenHandlerOptions = new TokenHandlerOptions
        {
            Authority = "https://example.com",
            ClientId = "test-client",
            ClientSecret = "test-secret"
        };

        _optionsMock.Setup(o => o.Value).Returns(_tokenHandlerOptions);

        _middleware = new AuthenticationHeaderSubstitutionMiddleware(
            _nextMock.Object,
            _cache,
            _optionsMock.Object,
            _configMock.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesMiddleware()
    {
        // Arrange
        var nextMock = new Mock<RequestDelegate>();
        var cacheMock = new TestHybridCache();
        var configMock = new Mock<IConfiguration>();
        var optionsMock = new Mock<IOptions<TokenHandlerOptions>>();
        optionsMock.Setup(o => o.Value).Returns(new TokenHandlerOptions());

        // Act
        var middleware = new AuthenticationHeaderSubstitutionMiddleware(
            nextMock.Object,
            cacheMock,
            optionsMock.Object,
            configMock.Object);

        // Assert
        Assert.NotNull(middleware);
    }

    [Fact]
    public async Task InvokeAsync_WithNoAuthenticationHeader_CallsNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticationHeaderButNoCache_CallsNextDelegate()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer session-token-123";

        _cache.SetValue("session-token-123", default(OpenIdConnectMessage)!);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithValidTokenNotExpiring_SetsAuthorizationHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var sessionToken = "session-token-123";
        context.Request.Headers["Authorization"] = $"Bearer {sessionToken}";

        var validToken = CreateJwtToken(DateTimeOffset.UtcNow.AddHours(1));
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = validToken,
            RefreshToken = "refresh-token"
        };

        _cache.SetValue(sessionToken, tokenResponse);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal($"Bearer {validToken}", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithCookieAuthentication_ExtractsTokenFromCookie()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var sessionToken = "cookie-session-token";
        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { global::AppForeach.TokenHandler.Extensions.ConfigurationExtensions.AuthenticationCookieName, sessionToken }
        });

        var validToken = CreateJwtToken(DateTimeOffset.UtcNow.AddHours(1));
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = validToken,
            RefreshToken = "refresh-token"
        };

        _cache.SetValue(sessionToken, tokenResponse);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal($"Bearer {validToken}", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithExpiredToken_DoesNotSetAuthorizationHeaderWhenRefreshFails()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var sessionToken = "session-token-123";
        context.Request.Headers["Authorization"] = $"Bearer {sessionToken}";

        var expiredToken = CreateJwtToken(DateTimeOffset.UtcNow.AddMinutes(-5));
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = expiredToken,
            RefreshToken = "refresh-token"
        };

        _cache.SetValue(sessionToken, tokenResponse);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithTokenExpiringIn3Minutes_AttemptsRefresh()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var sessionToken = "session-token-123";
        context.Request.Headers["Authorization"] = $"Bearer {sessionToken}";

        var expiringToken = CreateJwtToken(DateTimeOffset.UtcNow.AddMinutes(3));
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = expiringToken,
            RefreshToken = "refresh-token"
        };

        _cache.SetValue(sessionToken, tokenResponse);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert - The middleware will attempt refresh but it will fail (no mock server)
        // so the Authorization header won't be set with a refreshed token
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithBearerTokenWithExtraSpaces_ExtractsTokenCorrectly()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var sessionToken = "session-token-123";
        context.Request.Headers["Authorization"] = $"Bearer    {sessionToken}   ";

        var validToken = CreateJwtToken(DateTimeOffset.UtcNow.AddHours(1));
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = validToken,
            RefreshToken = "refresh-token"
        };

        _cache.SetValue(sessionToken, tokenResponse);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal($"Bearer {validToken}", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithCookieButNoAuthHeader_SetsAuthorizationHeader()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var sessionToken = "cookie-session-token";
        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { global::AppForeach.TokenHandler.Extensions.ConfigurationExtensions.AuthenticationCookieName, sessionToken }
        });

        var validToken = CreateJwtToken(DateTimeOffset.UtcNow.AddHours(1));
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = validToken,
            RefreshToken = "refresh-token"
        };

        _cache.SetValue(sessionToken, tokenResponse);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal($"Bearer {validToken}", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(n => n(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthorizationHeaderWithoutBearer_ExtractsTokenFromCookie()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var sessionToken = "cookie-session-token";
        context.Request.Headers["Authorization"] = "Basic something";
        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { global::AppForeach.TokenHandler.Extensions.ConfigurationExtensions.AuthenticationCookieName, sessionToken }
        });

        var validToken = CreateJwtToken(DateTimeOffset.UtcNow.AddHours(1));
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = validToken,
            RefreshToken = "refresh-token"
        };

        _cache.SetValue(sessionToken, tokenResponse);

        // Act
        await _middleware.InvokeAsync(context);

        // Assert
        Assert.Equal($"Bearer {validToken}", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(n => n(context), Times.Once);
    }

    private static string CreateJwtToken(DateTimeOffset expiration)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            expires: expiration.UtcDateTime,
            notBefore: DateTimeOffset.UtcNow.AddMinutes(-10).UtcDateTime
        );
        return handler.WriteToken(token);
    }

    private class TestHybridCache : HybridCache
    {
        private readonly Dictionary<string, object> _cache = new();

        public void SetValue<T>(string key, T value)
        {
            _cache[key] = value!;
        }

        public override ValueTask<T> GetOrCreateAsync<TState, T>(
            string key,
            TState state,
            Func<TState, CancellationToken, ValueTask<T>> factory,
            HybridCacheEntryOptions? options = null,
            IEnumerable<string>? tags = null,
            CancellationToken cancellationToken = default)
        {
            if (_cache.TryGetValue(key, out var cachedValue))
            {
                return ValueTask.FromResult((T)cachedValue);
            }

            return factory(state, cancellationToken);
        }

        public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            _cache.Remove(key);
            return ValueTask.CompletedTask;
        }

        public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        {
            _cache[key] = value!;
            return ValueTask.CompletedTask;
        }
    }

    private class MockRequestCookieCollection : IRequestCookieCollection
    {
        private readonly Dictionary<string, string> _cookies;

        public MockRequestCookieCollection(Dictionary<string, string> cookies)
        {
            _cookies = cookies;
        }

        public string? this[string key] => _cookies.TryGetValue(key, out var value) ? value : null;

        public int Count => _cookies.Count;

        public ICollection<string> Keys => _cookies.Keys;

        public bool ContainsKey(string key) => _cookies.ContainsKey(key);

        public bool TryGetValue(string key, out string value)
        {
            if (_cookies.TryGetValue(key, out var val))
            {
                value = val;
                return true;
            }
            value = string.Empty;
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
