using AppForeach.TokenHandler.Controllers;
using AppForeach.TokenHandler.Extensions;
using AppForeach.TokenHandler.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AppForeach.TokenHandler.Tests.Controllers;

public class TokenHandlerControllerTests
{
    private readonly Mock<IAuthenticationService> _authenticationServiceMock;
    private readonly Mock<ITokenStorageService> _tokenStorageServiceMock;
    private readonly TokenHandlerController _controller;

    public TokenHandlerControllerTests()
    {
        _authenticationServiceMock = new Mock<IAuthenticationService>();
        _tokenStorageServiceMock = new Mock<ITokenStorageService>();
        _controller = new TokenHandlerController(_tokenStorageServiceMock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = CreateServiceProvider(_authenticationServiceMock.Object);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private static IServiceProvider CreateServiceProvider(IAuthenticationService authenticationService)
    {
        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IAuthenticationService)))
            .Returns(authenticationService);
        return serviceProviderMock.Object;
    }

    [Fact]
    public void Authorize_WithDefaultReturnUrl_ReturnsChallengeResult()
    {
        // Act
        var result = _controller.Authorize();

        // Assert
        var challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal("/", challengeResult.Properties?.RedirectUri);
        Assert.Contains("oidc", challengeResult.AuthenticationSchemes);
    }

    [Fact]
    public void Authorize_WithCustomReturnUrl_ReturnsChallengeResultWithCustomUrl()
    {
        // Arrange
        var customReturnUrl = "/custom/path";

        // Act
        var result = _controller.Authorize(customReturnUrl);

        // Assert
        var challengeResult = Assert.IsType<ChallengeResult>(result);
        Assert.Equal(customReturnUrl, challengeResult.Properties?.RedirectUri);
        Assert.Contains("oidc", challengeResult.AuthenticationSchemes);
    }

    [Fact]
    public async Task Logout_SignsOutFromBothSchemes_ReturnsRedirect()
    {
        // Act
        var result = await _controller.Logout();

        // Assert
        var redirectResult = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/", redirectResult.Url);

        _authenticationServiceMock.Verify(
            x => x.SignOutAsync(It.IsAny<HttpContext>(), "Cookies", null),
            Times.Once);

        _authenticationServiceMock.Verify(
            x => x.SignOutAsync(
                It.IsAny<HttpContext>(),
                "oidc",
                It.Is<AuthenticationProperties>(p => p.RedirectUri == "/")),
            Times.Once);
    }

    [Fact]
    public async Task Logout_WithTrackedSession_RemovesCachedSession()
    {
        _controller.HttpContext.Request.Cookies = new MockRequestCookieCollection(new Dictionary<string, string>
        {
            [ConfigurationExtensions.AuthenticationCookieName] = "session-123"
        });

        await _controller.Logout();

        _tokenStorageServiceMock.Verify(
            store => store.RemoveAsync("session-123", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private sealed class MockRequestCookieCollection : IRequestCookieCollection
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
