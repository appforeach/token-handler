using System.Net.Http.Headers;
using System.Text.Json;
using Yarp.ReverseProxy.Transforms;

namespace Poc.Yarp;

public class TokenExchangeTransform : RequestTransform
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TokenExchangeTransform> _logger;

    public TokenExchangeTransform(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<TokenExchangeTransform> logger)
    {
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
        
        try
        {
            var newToken = await ExchangeTokenAsync(subjectToken);
            
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

    private async Task<string?> ExchangeTokenAsync(string subjectToken)
    {
        var keycloakUrl = _configuration["Keycloak:Url"];
        var realm = _configuration["Keycloak:Realm"];
        var clientId = _configuration["Keycloak:ClientId"];
        var clientSecret = _configuration["Keycloak:ClientSecret"];
        var targetAudience = "api";

        var tokenEndpoint = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token";

        var httpClient = _httpClientFactory.CreateClient();
        
        var requestBody = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["subject_token"] = subjectToken,
            ["subject_token_type"] = "urn:ietf:params:oauth:token-type:access_token",
            ["audience"] = targetAudience,
            ["requested_token_type"] = "urn:ietf:params:oauth:token-type:access_token"
        };

        var response = await httpClient.PostAsync(
            tokenEndpoint,
            new FormUrlEncodedContent(requestBody));

        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync();
        var tokenResponse = JsonSerializer.Deserialize<TokenExchangeResponse>(content);

        return tokenResponse?.AccessToken;
    }

    private class TokenExchangeResponse
    {
        [System.Text.Json.Serialization.JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }
}