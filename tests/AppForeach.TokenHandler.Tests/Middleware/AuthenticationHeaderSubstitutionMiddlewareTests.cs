using AppForeach.TokenHandler.Extensions;
using AppForeach.TokenHandler.Middleware;
using AppForeach.TokenHandler.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;

namespace AppForeach.TokenHandler.Tests.Middleware;

public class AuthenticationHeaderSubstitutionMiddlewareTests
{
    private readonly Mock<RequestDelegate> _nextMock;
    private readonly Mock<ITokenStorageService> _tokenStorageServiceMock;
    private readonly Mock<ITokenRefreshService> _tokenRefreshServiceMock;
    private readonly AuthenticationHeaderSubstitutionMiddleware _middleware;

    public AuthenticationHeaderSubstitutionMiddlewareTests()
    {
        _nextMock = new Mock<RequestDelegate>();
        _tokenStorageServiceMock = new Mock<ITokenStorageService>();
        _tokenRefreshServiceMock = new Mock<ITokenRefreshService>();

        _middleware = new AuthenticationHeaderSubstitutionMiddleware(
            _nextMock.Object,
            _tokenStorageServiceMock.Object,
            _tokenRefreshServiceMock.Object);
    }

    [Fact]
    public void Constructor_WithValidParameters_InitializesMiddleware()
    {
        var middleware = new AuthenticationHeaderSubstitutionMiddleware(
            Mock.Of<RequestDelegate>(),
            Mock.Of<ITokenStorageService>(),
            Mock.Of<ITokenRefreshService>());

        Assert.NotNull(middleware);
    }

    [Fact]
    public async Task InvokeAsync_WithNoAuthenticationHeader_CallsNextDelegate()
    {
        var context = new DefaultHttpContext();

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthenticationHeaderButNoCachedToken_CallsNextDelegate()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["Authorization"] = "Bearer session-token-123";
        _tokenStorageServiceMock
            .Setup(store => store.GetAsync("session-token-123", It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<OpenIdConnectMessage?>(null));

        await _middleware.InvokeAsync(context);

        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithValidTokenNotExpiring_SetsAuthorizationHeader()
    {
        var context = new DefaultHttpContext();
        var sessionToken = "session-token-123";
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = "valid-access-token",
            RefreshToken = "refresh-token"
        };

        context.Request.Headers["Authorization"] = $"Bearer {sessionToken}";
        _tokenStorageServiceMock
            .Setup(store => store.GetAsync(sessionToken, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<OpenIdConnectMessage?>(tokenResponse));
        _tokenRefreshServiceMock
            .Setup(service => service.ShouldRefresh(tokenResponse, It.IsAny<DateTimeOffset>()))
            .Returns(false);

        await _middleware.InvokeAsync(context);

        Assert.Equal("Bearer valid-access-token", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithCookieAuthentication_ExtractsTokenFromCookie()
    {
        var context = new DefaultHttpContext();
        var sessionToken = "cookie-session-token";
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = "valid-access-token",
            RefreshToken = "refresh-token"
        };

        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { ConfigurationExtensions.AuthenticationCookieName, sessionToken }
        });

        _tokenStorageServiceMock
            .Setup(store => store.GetAsync(sessionToken, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<OpenIdConnectMessage?>(tokenResponse));
        _tokenRefreshServiceMock
            .Setup(service => service.ShouldRefresh(tokenResponse, It.IsAny<DateTimeOffset>()))
            .Returns(false);

        await _middleware.InvokeAsync(context);

        Assert.Equal("Bearer valid-access-token", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithExpiringTokenAndRefreshFailure_LeavesOriginalHeader()
    {
        var context = new DefaultHttpContext();
        var sessionToken = "session-token-123";
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = "expiring-access-token",
            RefreshToken = "refresh-token"
        };

        context.Request.Headers["Authorization"] = $"Bearer {sessionToken}";
        _tokenStorageServiceMock
            .Setup(store => store.GetAsync(sessionToken, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<OpenIdConnectMessage?>(tokenResponse));
        _tokenRefreshServiceMock
            .Setup(service => service.ShouldRefresh(tokenResponse, It.IsAny<DateTimeOffset>()))
            .Returns(true);
        _tokenRefreshServiceMock
            .Setup(service => service.RefreshAsync(tokenResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenIdConnectMessage?)null);

        await _middleware.InvokeAsync(context);

        Assert.Equal($"Bearer {sessionToken}", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithExpiringToken_RefreshesAndStoresNewToken()
    {
        var context = new DefaultHttpContext();
        var sessionToken = "session-token-123";
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = "expiring-access-token",
            RefreshToken = "refresh-token"
        };
        var refreshedToken = new OpenIdConnectMessage
        {
            AccessToken = "refreshed-access-token",
            RefreshToken = "new-refresh-token"
        };

        context.Request.Headers["Authorization"] = $"Bearer {sessionToken}";
        _tokenStorageServiceMock
            .Setup(store => store.GetAsync(sessionToken, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<OpenIdConnectMessage?>(tokenResponse));
        _tokenRefreshServiceMock
            .Setup(service => service.ShouldRefresh(tokenResponse, It.IsAny<DateTimeOffset>()))
            .Returns(true);
        _tokenRefreshServiceMock
            .Setup(service => service.RefreshAsync(tokenResponse, It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshedToken);

        await _middleware.InvokeAsync(context);

        Assert.Equal("Bearer refreshed-access-token", context.Request.Headers["Authorization"].ToString());
        _tokenStorageServiceMock.Verify(
            store => store.StoreAsync(sessionToken, refreshedToken, It.IsAny<CancellationToken>()),
            Times.Once);
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithBearerTokenWithExtraSpaces_ExtractsTokenCorrectly()
    {
        var context = new DefaultHttpContext();
        var sessionToken = "session-token-123";
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = "valid-access-token",
            RefreshToken = "refresh-token"
        };

        context.Request.Headers["Authorization"] = $"Bearer    {sessionToken}   ";
        _tokenStorageServiceMock
            .Setup(store => store.GetAsync(sessionToken, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<OpenIdConnectMessage?>(tokenResponse));
        _tokenRefreshServiceMock
            .Setup(service => service.ShouldRefresh(tokenResponse, It.IsAny<DateTimeOffset>()))
            .Returns(false);

        await _middleware.InvokeAsync(context);

        Assert.Equal("Bearer valid-access-token", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithAuthorizationHeaderWithoutBearer_ExtractsTokenFromCookie()
    {
        var context = new DefaultHttpContext();
        var sessionToken = "cookie-session-token";
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = "valid-access-token",
            RefreshToken = "refresh-token"
        };

        context.Request.Headers["Authorization"] = "Basic something";
        context.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            { ConfigurationExtensions.AuthenticationCookieName, sessionToken }
        });

        _tokenStorageServiceMock
            .Setup(store => store.GetAsync(sessionToken, It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<OpenIdConnectMessage?>(tokenResponse));
        _tokenRefreshServiceMock
            .Setup(service => service.ShouldRefresh(tokenResponse, It.IsAny<DateTimeOffset>()))
            .Returns(false);

        await _middleware.InvokeAsync(context);

        Assert.Equal("Bearer valid-access-token", context.Request.Headers["Authorization"].ToString());
        _nextMock.Verify(next => next(context), Times.Once);
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
            if (_cookies.TryGetValue(key, out var storedValue))
            {
                value = storedValue;
                return true;
            }

            value = string.Empty;
            return false;
        }

        public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _cookies.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
