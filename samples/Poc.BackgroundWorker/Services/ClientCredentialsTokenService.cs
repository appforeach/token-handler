using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Poc.BackgroundWorker.Services;

/// <summary>
/// Service for acquiring tokens using Client Credentials Grant.
/// Reuses OpenIdConnectOptions from the existing token handler configuration.
/// </summary>
public interface IClientCredentialsTokenService
{
    /// <summary>
    /// Gets an access token for the specified audience using client credentials.
    /// </summary>
    Task<ClientCredentialsResult> GetAccessTokenAsync(
        string? audience = null,
        IEnumerable<string>? scopes = null,
        CancellationToken cancellationToken = default);
}

public class ClientCredentialsTokenService : IClientCredentialsTokenService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenIdConnectOptions _oidcOptions;
    private readonly ILogger<ClientCredentialsTokenService> _logger;

    public ClientCredentialsTokenService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
        ILogger<ClientCredentialsTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        // Get the "oidc" named options configured in Program.cs
        _oidcOptions = oidcOptionsMonitor.Get("oidc");
        _logger = logger;
    }

    public async Task<ClientCredentialsResult> GetAccessTokenAsync(
        string? audience = null,
        IEnumerable<string>? scopes = null,
        CancellationToken cancellationToken = default)
    {
        var tokenEndpoint = await GetTokenEndpointAsync(cancellationToken);
        
        if (string.IsNullOrEmpty(tokenEndpoint))
        {
            return ClientCredentialsResult.Failure("config_error", "Token endpoint not configured");
        }

        var clientId = _oidcOptions.ClientId;
        var clientSecret = _oidcOptions.ClientSecret;

        if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
        {
            return ClientCredentialsResult.Failure("config_error", "Client credentials not configured");
        }

        var body = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret
        };

        // Add audience if specified (for Keycloak, this targets a specific client)
        if (!string.IsNullOrEmpty(audience))
        {
            body["audience"] = audience;
        }

        // Add scopes if specified
        if (scopes?.Any() == true)
        {
            body["scope"] = string.Join(" ", scopes);
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("ClientCredentials");
            var content = new FormUrlEncodedContent(body);
            
            _logger.LogDebug("Requesting client credentials token from {TokenEndpoint}", tokenEndpoint);
            
            var response = await httpClient.PostAsync(tokenEndpoint, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Client credentials request failed: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);
                
                var errorResponse = JsonSerializer.Deserialize<TokenErrorResponse>(responseContent);
                return ClientCredentialsResult.Failure(
                    errorResponse?.Error ?? "request_failed",
                    errorResponse?.ErrorDescription ?? $"HTTP {response.StatusCode}");
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenResponse>(responseContent);
            
            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                return ClientCredentialsResult.Failure("invalid_response", "No access token in response");
            }

            _logger.LogInformation("Successfully acquired client credentials token (expires in {ExpiresIn}s)", 
                tokenResponse.ExpiresIn);
            
            return ClientCredentialsResult.Success(
                tokenResponse.AccessToken,
                tokenResponse.ExpiresIn,
                tokenResponse.TokenType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring client credentials token");
            return ClientCredentialsResult.Failure("exception", ex.Message);
        }
    }

    private async Task<string?> GetTokenEndpointAsync(CancellationToken cancellationToken)
    {
        // Try to get from configuration manager (cached discovery document)
        if (_oidcOptions.ConfigurationManager is not null)
        {
            try
            {
                var config = await _oidcOptions.ConfigurationManager.GetConfigurationAsync(cancellationToken);
                return config?.TokenEndpoint;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get token endpoint from discovery");
            }
        }

        // Fallback: construct from authority (Keycloak pattern)
        if (!string.IsNullOrEmpty(_oidcOptions.Authority))
        {
            var authority = _oidcOptions.Authority.TrimEnd('/');
            return $"{authority}/protocol/openid-connect/token";
        }

        return null;
    }
}

public record ClientCredentialsResult(
    bool IsSuccess,
    string? AccessToken,
    int? ExpiresIn,
    string? TokenType,
    string? Error,
    string? ErrorDescription)
{
    public static ClientCredentialsResult Success(string accessToken, int? expiresIn, string? tokenType) =>
        new(true, accessToken, expiresIn, tokenType, null, null);

    public static ClientCredentialsResult Failure(string error, string? description) =>
        new(false, null, null, null, error, description);
}

public record TokenResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("access_token")]
    public string? AccessToken { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("token_type")]
    public string? TokenType { get; init; }
}

public record TokenErrorResponse
{
    [System.Text.Json.Serialization.JsonPropertyName("error")]
    public string? Error { get; init; }

    [System.Text.Json.Serialization.JsonPropertyName("error_description")]
    public string? ErrorDescription { get; init; }
}
