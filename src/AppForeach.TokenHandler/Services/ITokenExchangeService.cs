namespace AppForeach.TokenHandler.Services;

/// <summary>
/// Service for exchanging access tokens using RFC 8693 Token Exchange.
/// Allows exchanging a subject token for an access token targeting a specific resource or audience.
/// </summary>
public interface ITokenExchangeService
{
    /// <summary>
    /// Exchanges the subject token for an access token targeting a specific resource identified by an absolute URL.
    /// </summary>
    /// <param name="subjectToken">The current access token to exchange.</param>
    /// <param name="resourceUrl">The absolute URL of the target resource.</param>
    /// <param name="scopes">Optional scopes to request for the new token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the exchanged access token or error information.</returns>
    Task<TokenExchangeResult> ExchangeForResourceAsync(
        string subjectToken,
        string resourceUrl,
        IEnumerable<string>? scopes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges the subject token for an access token targeting a specific audience (OAuth Client ID).
    /// </summary>
    /// <param name="subjectToken">The current access token to exchange.</param>
    /// <param name="audience">The target audience (OAuth Client ID).</param>
    /// <param name="scopes">Optional scopes to request for the new token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing the exchanged access token or error information.</returns>
    Task<TokenExchangeResult> ExchangeForAudienceAsync(
        string subjectToken,
        string audience,
        IEnumerable<string>? scopes = null,
        CancellationToken cancellationToken = default);
}
