using AppForeach.TokenHandler.Controllers;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AppForeach.TokenHandler.Tests.Controllers;

public class TokenHandlerControllerTests
{
    private readonly Mock<IAuthenticationService> _authenticationServiceMock;
    private readonly TokenHandlerController _controller;

    public TokenHandlerControllerTests()
    {
        _authenticationServiceMock = new Mock<IAuthenticationService>();
        _controller = new TokenHandlerController();

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
}
