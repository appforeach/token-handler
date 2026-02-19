using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AppForeach.TokenHandler.Services;

/// <summary>
/// Implementation of token exchange service using RFC 8693 Token Exchange.
/// Uses OpenIdConnectOptions for client credentials and authority configuration.
/// </summary>
public class TokenExchangeService : ITokenExchangeService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly OpenIdConnectOptions _oidcOptions;
    private readonly ILogger<TokenExchangeService> _logger;

    public TokenExchangeService(
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
        ILogger<TokenExchangeService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _oidcOptions = oidcOptionsMonitor.Get("oidc");
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<TokenExchangeResult> ExchangeForResourceAsync(
        string subjectToken,
        string resourceUrl,
        IEnumerable<string>? scopes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(resourceUrl);

        if (!Uri.TryCreate(resourceUrl, UriKind.Absolute, out _))
        {
            throw new ArgumentException("Resource URL must be an absolute URL.", nameof(resourceUrl));
        }

        var audience = ExtractAudienceFromResourceUrl(resourceUrl);
        var defaultScope = $"{audience}-audience";

        // TODO:  obtain audience from resource URL or configuration if needed, for now we will use a fixed audience
        var request = new TokenExchangeRequest
        {
            SubjectToken = subjectToken,
            Resource = resourceUrl,
            Audience = audience,
            Scopes = scopes ?? new[] { defaultScope }
        };

        return await ExecuteTokenExchangeAsync(request, cancellationToken);
    }

    private string ExtractAudienceFromResourceUrl(string resourceUrl)
    {
        // Simple heuristic: use the host part of the URL as the audience
        try
        {
            var uri = new Uri(resourceUrl);

            //TODO: extract to configuration based mapping if needed, for now we will use a simple heuristic
            if (uri.AbsoluteUri.Contains("http://localhost:5149", StringComparison.OrdinalIgnoreCase))
                return "api";

            if (uri.AbsoluteUri.Contains("http://localhost:5200", StringComparison.OrdinalIgnoreCase))
                return "internalapi";

            return "api";
        }
        catch (UriFormatException ex)
        {
            _logger.LogWarning(ex, "Failed to parse resource URL for audience extraction: {ResourceUrl}", resourceUrl);
            return resourceUrl; // Fallback to using the whole URL if parsing fails
        }
    }

    /// <inheritdoc />
    public async Task<TokenExchangeResult> ExchangeForAudienceAsync(
        string subjectToken,
        string audience,
        IEnumerable<string>? scopes = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(subjectToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        var request = new TokenExchangeRequest
        {
            SubjectToken = subjectToken,
            Audience = audience,
            Scopes = scopes
        };

        return await ExecuteTokenExchangeAsync(request, cancellationToken);
    }

    private async Task<TokenExchangeResult> ExecuteTokenExchangeAsync(
        TokenExchangeRequest request,
        CancellationToken cancellationToken)
    {
        var tokenEndpoint = await GetTokenEndpointAsync(cancellationToken);

        if (string.IsNullOrEmpty(tokenEndpoint))
        {
            _logger.LogError("Failed to retrieve token endpoint from OpenID Connect configuration");
            return TokenExchangeResult.Failure("configuration_error", "Token endpoint not available");
        }

        var clientId = _oidcOptions.ClientId;
        var clientSecret = _oidcOptions.ClientSecret;

        if (string.IsNullOrEmpty(clientId))
        {
            _logger.LogError("ClientId is not configured in OpenIdConnectOptions");
            return TokenExchangeResult.Failure("configuration_error", "ClientId is not configured");
        }

        if (string.IsNullOrEmpty(clientSecret))
        {
            _logger.LogError("ClientSecret is not configured in OpenIdConnectOptions");
            return TokenExchangeResult.Failure("configuration_error", "ClientSecret is not configured");
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient("TokenExchange");

            var requestBody = BuildTokenExchangeRequestBody(request, clientId, clientSecret);

            _logger.LogDebug("Executing token exchange request to {TokenEndpoint}", tokenEndpoint);

            var response = await httpClient.PostAsync(
                tokenEndpoint,
                new FormUrlEncodedContent(requestBody),
                cancellationToken);

            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Token exchange failed with status {StatusCode}: {Content}",
                    response.StatusCode, content);

                var errorResponse = JsonSerializer.Deserialize<TokenExchangeResponse>(content);
                return TokenExchangeResult.Failure(
                    errorResponse?.Error ?? "token_exchange_failed",
                    errorResponse?.ErrorDescription ?? $"HTTP {response.StatusCode}");
            }

            var tokenResponse = JsonSerializer.Deserialize<TokenExchangeResponse>(content);

            if (tokenResponse is null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                _logger.LogError("Token exchange response did not contain an access token");
                return TokenExchangeResult.Failure("invalid_response", "No access token in response");
            }

            _logger.LogInformation("Token exchange completed successfully");
            return TokenExchangeResult.Success(tokenResponse);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error during token exchange");
            return TokenExchangeResult.Failure("http_error", ex.Message);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse token exchange response");
            return TokenExchangeResult.Failure("parse_error", "Invalid JSON response from token endpoint");
        }
    }

    private Dictionary<string, string> BuildTokenExchangeRequestBody(
        TokenExchangeRequest request,
        string clientId,
        string clientSecret)
    {
        var body = new Dictionary<string, string>
        {
            ["grant_type"] = TokenExchangeConstants.GrantType,
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["subject_token"] = request.SubjectToken,
            ["subject_token_type"] = request.SubjectTokenType,
            ["requested_token_type"] = request.RequestedTokenType
        };

        // Add resource (for resource-based exchange) TODO: figure out this
        if (!string.IsNullOrEmpty(request.Resource))
        {
            body["resource"] = request.Resource;
        }

        // Add audience (for audience-based exchange)
        if (!string.IsNullOrEmpty(request.Audience))
        {
            body["audience"] = request.Audience;
        }

        // Add scopes if specified
        if (request.Scopes?.Any() == true)
        {
            body["scope"] = string.Join(" ", request.Scopes);
        }

        return body;
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
                _logger.LogWarning(ex, "Failed to get token endpoint from configuration manager");
            }
        }

        // Fallback: construct from authority
        if (!string.IsNullOrEmpty(_oidcOptions.Authority))
        {
            var authority = _oidcOptions.Authority.TrimEnd('/');

            // Check if this looks like a Keycloak URL (contains /realms/)
            if (authority.Contains("/realms/"))
            {
                return $"{authority}/protocol/openid-connect/token";
            }

            // Standard OpenID Connect discovery endpoint pattern
            return $"{authority}/protocol/openid-connect/token";
        }

        return null;
    }
}
