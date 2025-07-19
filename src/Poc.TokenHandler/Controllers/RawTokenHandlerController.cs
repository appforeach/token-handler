using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Security.Cryptography;
using System.Web;
using Microsoft.Extensions.Caching.Hybrid;
using Poc.TokenHandler.Models;
using Poc.TokenHandler.Extensions;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;

namespace Poc.Yarp.Token_Handler.Controllers;

[ApiController]
[Route("[controller]")]
[Obsolete("This direct oauth callback handler approach was applied as a first draft")]
public class RawTokenHandlerController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly HybridCache _cache;

    public RawTokenHandlerController(IHttpClientFactory httpClientFactory, IConfiguration config, HybridCache cache)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _cache = cache;
    }

    // Init the PKCE flow
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize([FromQuery] string? redirectUri = null)
    {
        // Generate PKCE code verifier and challenge
        var codeVerifier = Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var codeChallenge = Base64UrlEncode(Sha256(codeVerifier));
        var state = Guid.NewGuid().ToString();
        var nonce = Guid.NewGuid().ToString();

        // Store codeVerifier in temp session (for demo, use in-memory, production use distributed cache)

        await _cache.SetAsync($"pkce_{state}", codeVerifier);

        var keycloakUrl = _config["Keycloak:Url"] ?? string.Empty;
        var realm = _config["Keycloak:Realm"] ?? string.Empty;
        var clientId = _config["Keycloak:ClientId"] ?? string.Empty;
        var callback = redirectUri ?? _config["Keycloak:PkceRedirectUri"] ?? "http://localhost:5198/tokenhandler/callback";

        var query = HttpUtility.ParseQueryString(string.Empty);
        query["client_id"] = clientId;
        query["response_type"] = "code";
        query["scope"] = "openid profile email";
        query["redirect_uri"] = callback;
        query["state"] = state;
        query["nonce"] = nonce;
        query["code_challenge"] = codeChallenge;
        query["code_challenge_method"] = "S256";

        var authUrl = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/auth?{query}";

        return Redirect(authUrl);
    }

    // Callback endpoint to handle the PKCE flow response
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code, [FromQuery] string state, [FromQuery] string? redirectUri = null)
    {
        var codeVerifier = await _cache.GetOrDefautAsync($"pkce_{state}", default(string));

        if (string.IsNullOrEmpty(codeVerifier))
            return BadRequest("Invalid PKCE state");

        var keycloakUrl = _config["Keycloak:Url"] ?? string.Empty;
        var realm = _config["Keycloak:Realm"] ?? string.Empty;
        var clientId = _config["Keycloak:ClientId"] ?? string.Empty;
        var clientSecret = _config["Keycloak:ClientSecret"] ?? string.Empty;
        var callbackUri = redirectUri ?? _config["Keycloak:PkceRedirectUri"] ?? "http://localhost:5198/tokenhandler/callback";

        var client = _httpClientFactory.CreateClient();
        var tokenEndpoint = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("grant_type", "authorization_code"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("code", code),
                new KeyValuePair<string, string>("redirect_uri", callbackUri),
                new KeyValuePair<string, string>("code_verifier", codeVerifier)
            });

        var response = await client.PostAsync(tokenEndpoint, content);

        if (!response.IsSuccessStatusCode)
            return Unauthorized();

        var json = await response.Content.ReadAsStringAsync();

        var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        tokenResponse.ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(tokenResponse.ExpiresIn);

        var sessionId = Guid.NewGuid().ToString();
        if (tokenResponse is not null)
        {
            await _cache.SetAsync(sessionId, tokenResponse);

            //TODO: discuss this;
            Response.Cookies.Append("session-id", sessionId, new CookieOptions { HttpOnly = false });
            //Response.Cookies.Append("session-id", sessionId, new CookieOptions { HttpOnly = false, SameSite = SameSiteMode.Strict });
        }
        // Optionally redirect to frontend with sessionId
        var frontendRedirect = _config["Frontend:RedirectAfterLogin"] ?? "/";
        return Redirect(frontendRedirect);
    }

    private static byte[] Sha256(string input)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
    }

    private static string Base64UrlEncode(byte[] arg)
    {
        return Convert.ToBase64String(arg).TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }


    #region Misc PoC utilities

    [HttpPost("login/plain")]
    public async Task<IActionResult> Login([FromForm] string username, [FromForm] string password)
    {
        var client = _httpClientFactory.CreateClient();
        var keycloakUrl = _config["Keycloak:Url"];
        var realm = _config["Keycloak:Realm"];
        var clientId = _config["Keycloak:ClientId"];
        var clientSecret = _config["Keycloak:ClientSecret"];

        var tokenEndpoint = $"{keycloakUrl}/realms/{realm}/protocol/openid-connect/token";
        var content = new FormUrlEncodedContent(new[]
        {
                new KeyValuePair<string, string>("grant_type", "password"),
                new KeyValuePair<string, string>("client_id", clientId),
                new KeyValuePair<string, string>("client_secret", clientSecret),
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });
        var response = await client.PostAsync(tokenEndpoint, content);
        if (!response.IsSuccessStatusCode)
            return Unauthorized();

        var json = await response.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(json);
        var token = doc.RootElement.GetProperty("access_token").GetString();
        var sessionId = Guid.NewGuid().ToString();

        await _cache.SetAsync(sessionId, token);

        Response.Cookies.Append("session-id", sessionId, new CookieOptions { HttpOnly = true, SameSite = SameSiteMode.Strict });
        return Ok(new { sessionId });
    }


    //[HttpGet("token/{sessionId}")]
    //public async Task<IActionResult> GetToken(string sessionId)
    //{
    //    var token = await _cache.GetTokenAsync(sessionId);

    //    if (token is not null)
    //        return Ok(new { token });

    //    return NotFound();
    //}

    #endregion
}
