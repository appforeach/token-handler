namespace AppForeach.TokenHandler.Services;

/// <summary>
/// Represents a token exchange request parameters.
/// </summary>
public class TokenExchangeRequest
{
    /// <summary>
    /// The subject token (access token) to exchange.
    /// </summary>
    public required string SubjectToken { get; init; }

    /// <summary>
    /// The type of the subject token. Defaults to access_token.
    /// </summary>
    public string SubjectTokenType { get; init; } = TokenExchangeConstants.AccessTokenType;

    /// <summary>
    /// The target resource URL (for resource-based exchange).
    /// </summary>
    public string? Resource { get; init; }

    /// <summary>
    /// The target audience/Client ID (for audience-based exchange).
    /// </summary>
    public string? Audience { get; init; }

    /// <summary>
    /// Optional scopes to request for the exchanged token.
    /// </summary>
    public IEnumerable<string>? Scopes { get; init; }

    /// <summary>
    /// The requested token type. Defaults to access_token.
    /// </summary>
    public string RequestedTokenType { get; init; } = TokenExchangeConstants.AccessTokenType;
}

/// <summary>
/// Constants for RFC 8693 Token Exchange.
/// </summary>
public static class TokenExchangeConstants
{
    /// <summary>
    /// Grant type for token exchange as defined in RFC 8693.
    /// </summary>
    public const string GrantType = "urn:ietf:params:oauth:grant-type:token-exchange";

    /// <summary>
    /// Token type identifier for access tokens.
    /// </summary>
    public const string AccessTokenType = "urn:ietf:params:oauth:token-type:access_token";

    /// <summary>
    /// Token type identifier for refresh tokens.
    /// </summary>
    public const string RefreshTokenType = "urn:ietf:params:oauth:token-type:refresh_token";

    /// <summary>
    /// Token type identifier for ID tokens.
    /// </summary>
    public const string IdTokenType = "urn:ietf:params:oauth:token-type:id_token";
}
