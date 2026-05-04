using AppForeach.TokenHandler.Extensions;
using AppForeach.TokenHandler.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;

namespace AppForeach.TokenHandler.Services;

public class TokenRefreshService : ITokenRefreshService
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly TokenHandlerOptions _tokenHandlerOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public TokenRefreshService(IHttpClientFactory httpClientFactory, IOptions<TokenHandlerOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _tokenHandlerOptions = options.Value;
    }

    public bool ShouldRefresh(OpenIdConnectMessage tokenResponse, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(tokenResponse);

        if (string.IsNullOrWhiteSpace(tokenResponse.AccessToken))
        {
            return false;
        }

        var jwt = _tokenHandler.ReadJwtToken(tokenResponse.AccessToken);
        return jwt.ValidTo <= now.Add(_tokenHandlerOptions.RefreshBeforeExpirationInMinutes).UtcDateTime;
    }

    public async Task<OpenIdConnectMessage?> RefreshAsync(OpenIdConnectMessage tokenResponse, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(tokenResponse);

        if (string.IsNullOrWhiteSpace(tokenResponse.RefreshToken))
        {
            return null;
        }

        var tokenEndpoint = $"{_tokenHandlerOptions.Authority.TrimEnd('/')}/protocol/openid-connect/token";

        var request = new HttpRequestMessage(HttpMethod.Post, tokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = tokenResponse.RefreshToken,
                ["client_id"] = _tokenHandlerOptions.ClientId,
                ["client_secret"] = _tokenHandlerOptions.ClientSecret
            })
        };

        var httpClient = _httpClientFactory.CreateClient(ConfigurationExtensions.TokenExchangeHttpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var refreshedToken = JsonSerializer.Deserialize<OAuthTokenResponse>(json, SerializerOptions);
        if (refreshedToken is null || string.IsNullOrWhiteSpace(refreshedToken.AccessToken))
        {
            return null;
        }

        refreshedToken.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(refreshedToken.ExpiresIn);

        return new OpenIdConnectMessage
        {
            AccessToken = refreshedToken.AccessToken,
            RefreshToken = string.IsNullOrWhiteSpace(refreshedToken.RefreshToken) ? tokenResponse.RefreshToken : refreshedToken.RefreshToken,
            IdToken = string.IsNullOrWhiteSpace(refreshedToken.IdToken) ? tokenResponse.IdToken : refreshedToken.IdToken,
            TokenType = string.IsNullOrWhiteSpace(refreshedToken.TokenType) ? tokenResponse.TokenType : refreshedToken.TokenType,
            Scope = string.IsNullOrWhiteSpace(refreshedToken.Scope) ? tokenResponse.Scope : refreshedToken.Scope,
            ExpiresIn = refreshedToken.ExpiresIn > 0
                ? refreshedToken.ExpiresIn.ToString(CultureInfo.InvariantCulture)
                : tokenResponse.ExpiresIn
        };
    }
}
