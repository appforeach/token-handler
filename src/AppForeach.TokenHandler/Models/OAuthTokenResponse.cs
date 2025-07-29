using System.Text.Json.Serialization;

namespace AppForeach.TokenHandler.Models;
public class OAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;
    [JsonPropertyName("id_token")]
    public string IdToken { get; set; } = string.Empty;
    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;
    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
    [JsonPropertyName("refresh_expires_in")]
    public int RefreshExpiresIn { get; set; }
    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;

    //custom property to store the expiration time. Reconsider later.
    public DateTimeOffset ExpiresAt { get; set; }
}