using Microsoft.Extensions.Caching.Hybrid;
using Poc.Yarp.Token_Handler.Models;

namespace Poc.Yarp.Token_Handler.Middleware;

public class AuthenticationHeaderSubstitutionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly HybridCache _cache;
    const string AuthenticationHeaderName = "Authorization";

    public AuthenticationHeaderSubstitutionMiddleware(RequestDelegate next, HybridCache cacheService)
    {
        _next = next;
        _cache = cacheService;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(AuthenticationHeaderName))
        {
            var authentiationHeader = context.Request.Headers[AuthenticationHeaderName].ToString();

            if (authentiationHeader.StartsWith("Bearer"))
            {
                var sessionToken = authentiationHeader.Substring("Bearer ".Length).Trim();

                var tokenResponse = await _cache.GetOrDefautAsync<OAuthTokenResponse>(sessionToken, default);

                if (tokenResponse is not null)
                    context.Request.Headers[AuthenticationHeaderName] = $"Bearer {tokenResponse.AccessToken}";
            }
        }

        await _next(context);
    }
}
