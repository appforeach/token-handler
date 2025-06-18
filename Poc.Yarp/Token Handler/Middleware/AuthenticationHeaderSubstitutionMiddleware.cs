using Microsoft.Extensions.Caching.Hybrid;
using Poc.Yarp.Token_Handler.Models;

namespace Poc.Yarp.Token_Handler.Middleware;

public class AuthenticationHeaderSubstitutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HybridCache _cache;
    private readonly IConfiguration _config;

    const string AuthenticationHeaderName = "Authorization";

    public AuthenticationHeaderSubstitutionMiddleware(RequestDelegate next, HybridCache cacheService, IConfiguration config)
    {
        _next = next;
        _cache = cacheService;
        _config = config;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(AuthenticationHeaderName))
        {
            var authenticationHeader = context.Request.Headers[AuthenticationHeaderName].ToString();

            if (authenticationHeader.StartsWith("Bearer"))
            {
                var sessionToken = authenticationHeader.Substring("Bearer ".Length).Trim();

                var tokenResponse = await _cache.GetOrDefautAsync<OAuthTokenResponse>(sessionToken, default);

                if (tokenResponse is not null)
                {
                    // Check if token is expired or about to expire (e.g., within 1 minute)
                    var now = DateTimeOffset.UtcNow;
                    if (tokenResponse.ExpiresAt <= now.AddMinutes(4))
                    {
                        // Attempt to refresh the token
                        var refreshedToken = await RefreshTokenAsync(tokenResponse.RefreshToken);
                        if (refreshedToken is not null)
                        {
                            tokenResponse = refreshedToken;
                            // Update cache with new token
                            await _cache.SetAsync(sessionToken, tokenResponse, default);
                        }
                    }

                    context.Request.Headers[AuthenticationHeaderName] = $"Bearer {tokenResponse.AccessToken}";
                }
            }
        }

        await _next(context);
    }

    // Assumes you have a method to refresh the token using the refresh token
    private async Task<OAuthTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var keycloakUrl = _config["Keycloak:Url"] ?? string.Empty;
        var realm = _config["Keycloak:Realm"] ?? string.Empty;

        var clientId = _config["Keycloak:ClientId"] ?? string.Empty;
        var clientSecret = _config["Keycloak:ClientSecret"] ?? string.Empty;

        var tokenEndpoint = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token";

        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", clientId },
                { "client_secret", clientSecret }
            })
        };

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
            return null;

        var json = await response.Content.ReadAsStringAsync();
        var tokenResponse = System.Text.Json.JsonSerializer.Deserialize<OAuthTokenResponse>(json, new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (tokenResponse != null)
        {
            // Set the absolute expiration time
            tokenResponse.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        }

        return tokenResponse;
    }
}
