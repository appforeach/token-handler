using System.Text.Json.Serialization;

namespace AppForeach.TokenHandler.Services;

/// <summary>
/// Represents the result of a token exchange operation.
/// </summary>
public class TokenExchangeResult
{
    /// <summary>
    /// Indicates whether the token exchange was successful.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// The exchanged access token (if successful).
    /// </summary>
    public string? AccessToken { get; init; }

    /// <summary>
    /// The token type (typically "Bearer").
    /// </summary>
    public string? TokenType { get; init; }

    /// <summary>
    /// The token expiration time in seconds.
    /// </summary>
    public int? ExpiresIn { get; init; }

    /// <summary>
    /// The issued token type.
    /// </summary>
    public string? IssuedTokenType { get; init; }

    /// <summary>
    /// The scopes granted for the token.
    /// </summary>
    public string? Scope { get; init; }

    /// <summary>
    /// The refresh token (if issued).
    /// </summary>
    public string? RefreshToken { get; init; }

    /// <summary>
    /// Error code (if the exchange failed).
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Error description (if the exchange failed).
    /// </summary>
    public string? ErrorDescription { get; init; }

    /// <summary>
    /// Creates a successful token exchange result.
    /// </summary>
    public static TokenExchangeResult Success(TokenExchangeResponse response) => new()
    {
        IsSuccess = true,
        AccessToken = response.AccessToken,
        TokenType = response.TokenType,
        ExpiresIn = response.ExpiresIn,
        IssuedTokenType = response.IssuedTokenType,
        Scope = response.Scope,
        RefreshToken = response.RefreshToken
    };

    /// <summary>
    /// Creates a failed token exchange result.
    /// </summary>
    public static TokenExchangeResult Failure(string error, string? errorDescription = null) => new()
    {
        IsSuccess = false,
        Error = error,
        ErrorDescription = errorDescription
    };
}

/// <summary>
/// Represents the raw token exchange response from the authorization server.
/// </summary>
public class TokenExchangeResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int? ExpiresIn { get; set; }

    [JsonPropertyName("issued_token_type")]
    public string? IssuedTokenType { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }

    [JsonPropertyName("error_description")]
    public string? ErrorDescription { get; set; }
}
