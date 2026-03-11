using AppForeach.TokenHandler.Services;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Moq;
using Moq.Protected;
using System.Net;
using System.Text.Json;

namespace AppForeach.TokenHandler.Tests.Services;

public class TokenExchangeServiceTests
{
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IOptionsMonitor<OpenIdConnectOptions>> _oidcOptionsMonitorMock;
    private readonly Mock<ILogger<TokenExchangeService>> _loggerMock;
    private readonly OpenIdConnectOptions _oidcOptions;

    public TokenExchangeServiceTests()
    {
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _oidcOptionsMonitorMock = new Mock<IOptionsMonitor<OpenIdConnectOptions>>();
        _loggerMock = new Mock<ILogger<TokenExchangeService>>();

        _oidcOptions = new OpenIdConnectOptions
        {
            ClientId = "test-client-id",
            ClientSecret = "test-client-secret",
            Authority = "https://auth.example.com/realms/test"
        };

        _oidcOptionsMonitorMock
            .Setup(x => x.Get("oidc"))
            .Returns(_oidcOptions);
    }

    private TokenExchangeService CreateService() =>
        new(_httpClientFactoryMock.Object, _oidcOptionsMonitorMock.Object, _loggerMock.Object);

    private HttpClient CreateMockHttpClient(HttpResponseMessage response)
    {
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response);

        return new HttpClient(handlerMock.Object);
    }

    #region ExchangeForResourceAsync Tests

    [Fact]
    public async Task ExchangeForResourceAsync_WithValidInputs_ReturnsSuccess()
    {
        // Arrange
        var tokenResponse = new TokenExchangeResponse
        {
            AccessToken = "new-access-token",
            TokenType = "Bearer",
            ExpiresIn = 3600
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
        };

        var httpClient = CreateMockHttpClient(response);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.ExchangeForResourceAsync(
            "subject-token",
            "https://api.example.com/resource");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.AccessToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExchangeForResourceAsync_WithNullOrEmptySubjectToken_ThrowsArgumentException(string? subjectToken)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeForResourceAsync(subjectToken!, "https://api.example.com"));
    }


    [Theory]
    [InlineData(null)]
    public async Task ExchangeForResourceAsync_WithNullOrEmptySubjectToken_ThrowsArgumentNullException(string? subjectToken)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ExchangeForResourceAsync(subjectToken!, "https://api.example.com"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExchangeForResourceAsync_WithNullOrEmptyResourceUrl_ThrowsArgumentException(string? resourceUrl)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeForResourceAsync("subject-token", resourceUrl!));
    }

    [InlineData(null)]
    public async Task ExchangeForResourceAsync_WithNullOrEmptyResourceUrl_ThrowsArgumentNullException(string? resourceUrl)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ExchangeForResourceAsync("subject-token", resourceUrl!));
    }

    [Fact]
    public async Task ExchangeForResourceAsync_WithRelativeUrl_ThrowsArgumentException()
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeForResourceAsync("subject-token", "/relative/path"));

        Assert.Contains("absolute URL", ex.Message);
    }

    [Fact]
    public async Task ExchangeForResourceAsync_WithCustomScopes_UsesProvidedScopes()
    {
        // Arrange
        var tokenResponse = new TokenExchangeResponse { AccessToken = "token" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
        };

        var httpClient = CreateMockHttpClient(response);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();
        var scopes = new[] { "scope1", "scope2" };

        // Act
        var result = await service.ExchangeForResourceAsync(
            "subject-token",
            "https://api.example.com",
            scopes);

        // Assert
        Assert.True(result.IsSuccess);
    }

    #endregion

    #region ExchangeForAudienceAsync Tests

    [Fact]
    public async Task ExchangeForAudienceAsync_WithValidInputs_ReturnsSuccess()
    {
        // Arrange
        var tokenResponse = new TokenExchangeResponse
        {
            AccessToken = "new-access-token",
            TokenType = "Bearer"
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
        };

        var httpClient = CreateMockHttpClient(response);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.ExchangeForAudienceAsync("subject-token", "target-audience");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new-access-token", result.AccessToken);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExchangeForAudienceAsync_WithNullOrEmptySubjectToken_ThrowsArgumentException(string? subjectToken)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeForAudienceAsync(subjectToken!, "audience"));
    }


    [Theory]
    [InlineData(null)]
    public async Task ExchangeForAudienceAsync_WithNullOrEmptySubjectToken_ThrowsArgumentNullException(string? subjectToken)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            service.ExchangeForAudienceAsync(subjectToken!, "audience"));
    }


    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExchangeForAudienceAsync_WithNullOrEmptyAudience_ThrowsArgumentException(string? audience)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeForAudienceAsync("subject-token", audience!));
    }
    [InlineData(null)]
    public async Task ExchangeForAudienceAsync_WithNullOrEmptyAudience_ThrowsArgumentNullException(string? audience)
    {
        // Arrange
        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            service.ExchangeForAudienceAsync("subject-token", audience!));
    }

    #endregion

    #region Configuration Error Tests

    [Fact]
    public async Task ExecuteTokenExchange_WithMissingClientId_ReturnsConfigurationError()
    {
        // Arrange
        _oidcOptions.ClientId = null;
        var service = CreateService();

        // Act
        var result = await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_error", result.Error);
        Assert.Contains("ClientId", result.ErrorDescription);
    }

    [Fact]
    public async Task ExecuteTokenExchange_WithMissingClientSecret_ReturnsConfigurationError()
    {
        // Arrange
        _oidcOptions.ClientSecret = null;
        var service = CreateService();

        // Act
        var result = await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_error", result.Error);
        Assert.Contains("ClientSecret", result.ErrorDescription);
    }

    [Fact]
    public async Task ExecuteTokenExchange_WithNoTokenEndpoint_ReturnsConfigurationError()
    {
        // Arrange
        _oidcOptions.Authority = null;
        _oidcOptions.ConfigurationManager = null;
        var service = CreateService();

        // Act
        var result = await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("configuration_error", result.Error);
        Assert.Contains("Token endpoint", result.ErrorDescription);
    }

    #endregion

    #region HTTP Error Tests

    [Fact]
    public async Task ExecuteTokenExchange_WithHttpError_ReturnsFailure()
    {
        // Arrange
        var errorResponse = new { error = "invalid_grant", error_description = "Token expired" };
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(JsonSerializer.Serialize(errorResponse))
        };

        var httpClient = CreateMockHttpClient(response);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_grant", result.Error);
    }

    [Fact]
    public async Task ExecuteTokenExchange_WithHttpRequestException_ReturnsHttpError()
    {
        // Arrange
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection failed"));

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("http_error", result.Error);
    }

    [Fact]
    public async Task ExecuteTokenExchange_WithInvalidJsonResponse_ReturnsParseError()
    {
        // Arrange
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("not valid json {{{")
        };

        var httpClient = CreateMockHttpClient(response);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("parse_error", result.Error);
    }

    [Fact]
    public async Task ExecuteTokenExchange_WithEmptyAccessToken_ReturnsInvalidResponse()
    {
        // Arrange
        var tokenResponse = new TokenExchangeResponse { AccessToken = null };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
        };

        var httpClient = CreateMockHttpClient(response);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act
        var result = await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_response", result.Error);
    }

    #endregion

    #region Token Endpoint Resolution Tests

    [Fact]
    public async Task GetTokenEndpoint_WithKeycloakAuthority_ReturnsKeycloakEndpoint()
    {
        // Arrange
        _oidcOptions.Authority = "https://keycloak.example.com/realms/myrealm";
        _oidcOptions.ConfigurationManager = null;

        var tokenResponse = new TokenExchangeResponse { AccessToken = "token" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
        };

        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act
        await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Contains("/protocol/openid-connect/token", capturedRequest.RequestUri?.ToString());
    }

    [Fact]
    public async Task GetTokenEndpoint_WithConfigurationManager_UsesDiscoveredEndpoint()
    {
        // Arrange
        var configManagerMock = new Mock<IConfigurationManager<OpenIdConnectConfiguration>>();
        var config = new OpenIdConnectConfiguration
        {
            TokenEndpoint = "https://discovered.example.com/token"
        };
        configManagerMock
            .Setup(x => x.GetConfigurationAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(config);

        _oidcOptions.ConfigurationManager = configManagerMock.Object;

        var tokenResponse = new TokenExchangeResponse { AccessToken = "token" };
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonSerializer.Serialize(tokenResponse))
        };

        HttpRequestMessage? capturedRequest = null;
        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(response);

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act
        await service.ExchangeForAudienceAsync("subject-token", "audience");

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.Equal("https://discovered.example.com/token", capturedRequest.RequestUri?.ToString());
    }

    #endregion

    #region Cancellation Tests

    [Fact]
    public async Task ExchangeForAudienceAsync_WithCancelledToken_ThrowsOperationCanceledException()
    {
        // Arrange
        var cts = new CancellationTokenSource();
        cts.Cancel();

        var handlerMock = new Mock<HttpMessageHandler>();
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new OperationCanceledException());

        var httpClient = new HttpClient(handlerMock.Object);
        _httpClientFactoryMock.Setup(x => x.CreateClient("TokenExchange")).Returns(httpClient);

        var service = CreateService();

        // Act & Assert
        await Assert.ThrowsAsync<TaskCanceledException>(() =>
            service.ExchangeForAudienceAsync("subject-token", "audience", null, cts.Token));
    }

    #endregion
}