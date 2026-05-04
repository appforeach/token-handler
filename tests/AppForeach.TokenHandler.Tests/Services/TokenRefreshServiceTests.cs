using AppForeach.TokenHandler.Extensions;
using AppForeach.TokenHandler.Services;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Moq.Protected;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace AppForeach.TokenHandler.Tests.Services;

public class TokenRefreshServiceTests
{
    [Fact]
    public void ShouldRefresh_ReturnsFalse_WhenTokenExpiresAfterThreshold()
    {
        var service = CreateService();
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = CreateJwtToken(DateTimeOffset.UtcNow.AddMinutes(10)),
            RefreshToken = "refresh-token"
        };

        var result = service.ShouldRefresh(tokenResponse, DateTimeOffset.UtcNow);

        Assert.False(result);
    }

    [Fact]
    public void ShouldRefresh_ReturnsTrue_WhenTokenExpiresInsideThreshold()
    {
        var service = CreateService();
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = CreateJwtToken(DateTimeOffset.UtcNow.AddMinutes(2)),
            RefreshToken = "refresh-token"
        };

        var result = service.ShouldRefresh(tokenResponse, DateTimeOffset.UtcNow);

        Assert.True(result);
    }

    [Fact]
    public async Task RefreshAsync_ReturnsRefreshedToken_WhenEndpointSucceeds()
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("""
                    {
                      "access_token": "new-access-token",
                      "refresh_token": "new-refresh-token",
                      "token_type": "Bearer",
                      "expires_in": 3600
                    }
                    """)
            });

        var httpClient = new HttpClient(handlerMock.Object);
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(ConfigurationExtensions.TokenExchangeHttpClientName))
            .Returns(httpClient);

        var options = Options.Create(new TokenHandlerOptions
        {
            Authority = "https://example.com/realms/poc",
            ClientId = "client-id",
            ClientSecret = "client-secret"
        });

        var service = new TokenRefreshService(httpClientFactory.Object, options);
        var tokenResponse = new OpenIdConnectMessage
        {
            AccessToken = CreateJwtToken(DateTimeOffset.UtcNow.AddMinutes(-1)),
            RefreshToken = "refresh-token"
        };

        var refreshedToken = await service.RefreshAsync(tokenResponse);

        Assert.NotNull(refreshedToken);
        Assert.Equal("new-access-token", refreshedToken.AccessToken);
        Assert.Equal("new-refresh-token", refreshedToken.RefreshToken);
    }

    private static TokenRefreshService CreateService()
    {
        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(factory => factory.CreateClient(ConfigurationExtensions.TokenExchangeHttpClientName))
            .Returns(new HttpClient(new Mock<HttpMessageHandler>().Object));

        return new TokenRefreshService(httpClientFactory.Object, Options.Create(new TokenHandlerOptions
        {
            Authority = "https://example.com/realms/poc",
            ClientId = "client-id",
            ClientSecret = "client-secret",
            RefreshBeforeExpirationInMinutes = TimeSpan.FromMinutes(4)
        }));
    }

    private static string CreateJwtToken(DateTimeOffset expiration)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            issuer: "test-issuer",
            audience: "test-audience",
            expires: expiration.UtcDateTime,
            notBefore: DateTimeOffset.UtcNow.AddMinutes(-10).UtcDateTime);
        return handler.WriteToken(token);
    }
}
