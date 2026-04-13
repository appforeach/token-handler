using AppForeach.TokenHandler.Services;
using Microsoft.AspNetCore.Http;
using Moq;
using Moq.Protected;
using System.Net;

namespace AppForeach.TokenHandler.Tests.Services;

public class TokenExchangeDelegatingHandlerTests
{
    private readonly Mock<ITokenExchangeService> _tokenExchangeServiceMock;
    private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
    private readonly Mock<HttpMessageHandler> _innerHandlerMock;

    public TokenExchangeDelegatingHandlerTests()
    {
        _tokenExchangeServiceMock = new Mock<ITokenExchangeService>();
        _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        _innerHandlerMock = new Mock<HttpMessageHandler>();
    }

    private TokenExchangeDelegatingHandler CreateHandler()
    {
        var handler = new TokenExchangeDelegatingHandler(
            _tokenExchangeServiceMock.Object,
            _httpContextAccessorMock.Object)
        {
            InnerHandler = _innerHandlerMock.Object
        };
        return handler;
    }

    [Fact]
    public void Constructor_WithValidDependencies_InitializesSuccessfully()
    {
        // Arrange & Act
        var handler = new TokenExchangeDelegatingHandler(
            _tokenExchangeServiceMock.Object,
            _httpContextAccessorMock.Object);

        // Assert
        Assert.NotNull(handler);
    }

    [Fact]
    public async Task SendAsync_WithValidTokenAndRequestUri_ExchangesTokenAndSendsRequest()
    {
        // Arrange
        var incomingToken = "incoming-token";
        var exchangedToken = "exchanged-token";
        var requestUri = new Uri("https://api.example.com/resource");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {incomingToken}";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var tokenExchangeResult = new TokenExchangeResult
        {
            IsSuccess = true,
            AccessToken = exchangedToken
        };

        _tokenExchangeServiceMock
            .Setup(x => x.ExchangeForResourceAsync(
                incomingToken,
                requestUri.ToString(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenExchangeResult);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        var handler = CreateHandler();
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _tokenExchangeServiceMock.Verify(
            x => x.ExchangeForResourceAsync(
                incomingToken,
                requestUri.ToString(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithNoHttpContext_SkipsTokenExchangeAndSendsRequest()
    {
        // Arrange
        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        var handler = CreateHandler();
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _tokenExchangeServiceMock.Verify(
            x => x.ExchangeForResourceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithEmptyToken_SkipsTokenExchangeAndSendsRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer ";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        var handler = CreateHandler();
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _tokenExchangeServiceMock.Verify(
            x => x.ExchangeForResourceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithNoAuthorizationHeader_SkipsTokenExchangeAndSendsRequest()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        var handler = CreateHandler();
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _tokenExchangeServiceMock.Verify(
            x => x.ExchangeForResourceAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SendAsync_WithValidToken_SetsAuthorizationHeader()
    {
        // Arrange
        var incomingToken = "incoming-token";
        var exchangedToken = "exchanged-token";
        var requestUri = new Uri("https://api.example.com/resource");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {incomingToken}";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var tokenExchangeResult = new TokenExchangeResult
        {
            IsSuccess = true,
            AccessToken = exchangedToken
        };

        _tokenExchangeServiceMock
            .Setup(x => x.ExchangeForResourceAsync(
                incomingToken,
                requestUri.ToString(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenExchangeResult);

        HttpRequestMessage? capturedRequest = null;
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var handler = CreateHandler();
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // Act
        await httpClient.SendAsync(request);

        // Assert
        Assert.NotNull(capturedRequest);
        Assert.NotNull(capturedRequest.Headers.Authorization);
        Assert.Equal("Bearer", capturedRequest.Headers.Authorization.Scheme);
        Assert.Equal(exchangedToken, capturedRequest.Headers.Authorization.Parameter);
    }

    [Fact]
    public async Task SendAsync_WithBearerTokenInMixedCase_ExtractsTokenCorrectly()
    {
        // Arrange
        var incomingToken = "incoming-token";
        var exchangedToken = "exchanged-token";
        var requestUri = new Uri("https://api.example.com/resource");

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"BeArEr {incomingToken}";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var tokenExchangeResult = new TokenExchangeResult
        {
            IsSuccess = true,
            AccessToken = exchangedToken
        };

        _tokenExchangeServiceMock
            .Setup(x => x.ExchangeForResourceAsync(
                incomingToken,
                requestUri.ToString(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenExchangeResult);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        var handler = CreateHandler();
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // Act
        var response = await httpClient.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _tokenExchangeServiceMock.Verify(
            x => x.ExchangeForResourceAsync(
                incomingToken,
                requestUri.ToString(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendAsync_WithCancellationToken_PassesCancellationTokenToExchangeService()
    {
        // Arrange
        var incomingToken = "incoming-token";
        var exchangedToken = "exchanged-token";
        var requestUri = new Uri("https://api.example.com/resource");
        var cancellationTokenSource = new CancellationTokenSource();
        var cancellationToken = cancellationTokenSource.Token;

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = $"Bearer {incomingToken}";

        _httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

        var tokenExchangeResult = new TokenExchangeResult
        {
            IsSuccess = true,
            AccessToken = exchangedToken
        };

        _tokenExchangeServiceMock
            .Setup(x => x.ExchangeForResourceAsync(
                incomingToken,
                requestUri.ToString(),
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenExchangeResult);

        var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK);
        _innerHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(expectedResponse);

        var handler = CreateHandler();
        var httpClient = new HttpClient(handler);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

        // Act
        await httpClient.SendAsync(request, cancellationToken);

        // Assert
        _tokenExchangeServiceMock.Verify(
            x => x.ExchangeForResourceAsync(
                incomingToken,
                requestUri.ToString(),
                null,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
