using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Poc.TokenHandler.Extensions;
using Poc.TokenHandler.Models;
using System.IdentityModel.Tokens.Jwt;

namespace Poc.TokenHandler.Middleware;
public class AuthenticationHeaderSubstitutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HybridCache _cache;
    private readonly IConfiguration _config;
    private readonly TokenHandlerOptions _tokenHandlerOptions;

    const string AuthenticationHeaderName = "Authorization";

    public AuthenticationHeaderSubstitutionMiddleware(RequestDelegate next, HybridCache cacheService, IOptions<TokenHandlerOptions> options, IConfiguration config)
    {
        _next = next;
        _cache = cacheService;
        _config = config;
        _tokenHandlerOptions = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(AuthenticationHeaderName) || context.Request.Cookies.ContainsKey(Extensions.ConfigurationExtensions.AuthenticationCookieName))
        {
            var authenticationHeader = context.Request.Headers[AuthenticationHeaderName].ToString();

            var sessionToken = authenticationHeader.StartsWith("Bearer") ?
                authenticationHeader.Substring("Bearer ".Length).Trim() :
                context.Request.Cookies[Extensions.ConfigurationExtensions.AuthenticationCookieName];

            var tokenResponse = await _cache.GetOrDefautAsync<OpenIdConnectMessage>(sessionToken, default);

            if (tokenResponse is not null)
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(tokenResponse.AccessToken);

                // Check if token is expired or about to expire (e.g., within 1 minute)
                var now = DateTimeOffset.UtcNow;
                if (jwt.ValidTo <= now.AddMinutes(4))
                {
                    // Attempt to refresh the token
                    var refreshedToken = await RefreshTokenAsync(tokenResponse.RefreshToken);
                    if (refreshedToken is not null)
                    {
                        await _cache.SetAsync(sessionToken, new OpenIdConnectMessage() { AccessToken = refreshedToken.AccessToken, RefreshToken = refreshedToken.RefreshToken }, default);
                        context.Request.Headers[AuthenticationHeaderName] = $"Bearer {refreshedToken.AccessToken}";
                    }
                }
                else
                {
                    context.Request.Headers[AuthenticationHeaderName] = $"Bearer {tokenResponse.AccessToken}";
                }
            }
        }

        await _next(context);
    }

    // Assumes you have a method to refresh the token using the refresh token
    private async Task<OAuthTokenResponse?> RefreshTokenAsync(string refreshToken)
    {
        var tokenEndpoint = $"{_tokenHandlerOptions.Authority}/protocol/openid-connect/token";

        using var httpClient = new HttpClient();
        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken },
                { "client_id", _tokenHandlerOptions.ClientId },
                { "client_secret", _tokenHandlerOptions.ClientSecret }
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