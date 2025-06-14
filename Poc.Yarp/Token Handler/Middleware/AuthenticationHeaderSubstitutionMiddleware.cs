using Microsoft.Extensions.Caching.Hybrid;

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

                //TODO: persist some model

                var token = await _cache.GetOrCreateAsync<string>(
                  sessionToken,
                  factory: _ => ValueTask.FromResult(string.Empty), // Default factory method
                  new HybridCacheEntryOptions() { Flags = HybridCacheEntryFlags.DisableUnderlyingData }
                );

                if (token is not null)
                    context.Request.Headers[AuthenticationHeaderName] = $"Bearer {token}";
            }
        }

        await _next(context);
    }
}
