using AppForeach.TokenHandler.Services;
using Microsoft.AspNetCore.Http;

namespace AppForeach.TokenHandler.Middleware;

public class AuthenticationHeaderSubstitutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ITokenStorageService _tokenStorageService;
    private readonly ITokenRefreshService _tokenRefreshService;

    const string AuthenticationHeaderName = "Authorization";

    public AuthenticationHeaderSubstitutionMiddleware(
        RequestDelegate next,
        ITokenStorageService tokenStorageService,
        ITokenRefreshService tokenRefreshService)
    {
        _next = next;
        _tokenStorageService = tokenStorageService;
        _tokenRefreshService = tokenRefreshService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(AuthenticationHeaderName) || context.Request.Cookies.ContainsKey(Extensions.ConfigurationExtensions.AuthenticationCookieName))
        {
            var authenticationHeader = context.Request.Headers[AuthenticationHeaderName].ToString();

            var sessionToken = authenticationHeader.StartsWith("Bearer") ?
                authenticationHeader.Substring("Bearer ".Length).Trim() :
                context.Request.Cookies[Extensions.ConfigurationExtensions.AuthenticationCookieName];

            if (string.IsNullOrWhiteSpace(sessionToken))
            {
                await _next(context);
                return;
            }

            var tokenResponse = await _tokenStorageService.GetAsync(sessionToken);

            if (tokenResponse is not null)
            {
                var now = DateTimeOffset.UtcNow;
                if (_tokenRefreshService.ShouldRefresh(tokenResponse, now))
                {
                    var refreshedToken = await _tokenRefreshService.RefreshAsync(tokenResponse);
                    if (refreshedToken is not null)
                    {
                        await _tokenStorageService.StoreAsync(sessionToken, refreshedToken);
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
}
