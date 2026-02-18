using AppForeach.TokenHandler.Services;
using System.Net.Http.Headers;
using Yarp.ReverseProxy.Transforms;

namespace Poc.Yarp;

public class TokenExchangeTransform : RequestTransform
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenExchangeTransform> _logger;
    private readonly ITokenExchangeService _tokenExchangeService;

    public TokenExchangeTransform(
        ITokenExchangeService tokenExchangeService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TokenExchangeTransform> logger)
    {
        _tokenExchangeService = tokenExchangeService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    public override async ValueTask ApplyAsync(RequestTransformContext context)
    {
        var authHeader = context.HttpContext.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
        {
            return;
        }

        var subjectToken = authHeader.Substring("Bearer ".Length);
        //var resourceUrl = context.ProxyRequest.RequestUri?.ToString() ?? string.Empty;
        var resourceUrl = context.DestinationPrefix;// context.HttpContext.Request.Path.Value?.ToString() ?? string.Empty;

        try
        {
            var newToken = await ExchangeTokenAsync(resourceUrl, subjectToken);

            if (!string.IsNullOrEmpty(newToken))
            {
                context.ProxyRequest.Headers.Authorization =
                    new AuthenticationHeaderValue("Bearer", newToken);
                _logger.LogInformation("Token exchanged successfully");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token exchange failed");
            // Optionally: context.HttpContext.Response.StatusCode = 401;
        }
    }

    private async Task<string?> ExchangeTokenAsync(string resourceUrl, string subjectToken)
    {
        var result = await _tokenExchangeService.ExchangeForResourceAsync(subjectToken, resourceUrl);

        return result.IsSuccess ? result.AccessToken : null;
    }
}