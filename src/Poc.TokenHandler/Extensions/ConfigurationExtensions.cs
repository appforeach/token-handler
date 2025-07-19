using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Poc.TokenHandler.Middleware;
using System.Diagnostics;

namespace Poc.TokenHandler.Extensions;
public static class ConfigurationExtensions
{
    public static IServiceCollection AddTokenHandler(this IServiceCollection services, Action<TokenHandlerOptions> overrideOptions)
    {
        var tokenHandlerOptions = new TokenHandlerOptions
        {
            Authority = "http://localhost:8080/realms/poc",
            ClientId = "poc-api",
            ClientSecret = "2ISb8zFHUU4Q5XZDd2xRN4LpkjMPz2mK",
            Realm = "poc"
        };

        if (overrideOptions is not null)
        {
            overrideOptions(tokenHandlerOptions);
        }

        services.Configure<TokenHandlerOptions>(options =>
        {
            options.Authority = tokenHandlerOptions.Authority;
            options.ClientId = tokenHandlerOptions.ClientId;
            options.ClientSecret = tokenHandlerOptions.ClientSecret;
            options.Realm = tokenHandlerOptions.Realm;
        });

        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // For development. In production, use Redis or SQL Server
        services.AddHybridCache();


        services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie(options =>
        {
            options.LoginPath = "/Account/Login/";
        })
       .AddOpenIdConnect("oidc", options =>
       {
           options.Authority = tokenHandlerOptions.Authority;
           options.ClientId = tokenHandlerOptions.ClientId;
           options.ClientSecret = tokenHandlerOptions.ClientSecret;
           options.ResponseType = OpenIdConnectResponseType.Code;

           options.SaveTokens = true;
           options.GetClaimsFromUserInfoEndpoint = true;

           //dev config only
           options.RequireHttpsMetadata = false;

           options.CallbackPath = "/signin-oidc"; // Default

           // Map claims if needed
           options.Scope.Add("openid");
           options.Scope.Add("profile");

           options.TokenValidationParameters = new TokenValidationParameters
           {
               NameClaimType = "preferred_username",
               RoleClaimType = "roles"
           };
           options.Events = new OpenIdConnectEvents
           {
               OnAuthorizationCodeReceived = async context =>
               {
                   // Your custom logic here before the token is redeemed
                   var code = context.ProtocolMessage.Code;
                   var redirectUri = context.ProtocolMessage.RedirectUri;

                   Debug.WriteLine($"Received auth code: {code}");

                   // You can call an external service or modify the token request

                   await Task.CompletedTask;
               },
               OnTokenValidated = async context =>
               {
                   var hybridCache = context.HttpContext.RequestServices.GetRequiredService<HybridCache>();
                   Debug.WriteLine($"Authenticated user: {context.Principal?.Identity?.Name}");

                   if (context.TokenEndpointResponse is null)
                       return;

                   var sessionId = Guid.NewGuid().ToString();
                   await hybridCache.SetAsync(sessionId, context.TokenEndpointResponse);

                   var httpContext = context.HttpContext;
                   httpContext.Response.Cookies.Append("session-id", sessionId, new CookieOptions
                   {
                       HttpOnly = !true, //dev
                                         // Secure = true, //dev
                                         // SameSite = SameSiteMode.Strict,
                                         // Expires = DateTimeOffset.UtcNow.AddHours(1)
                   });


                   context.Properties.RedirectUri = context.ProtocolMessage.RedirectUri ?? "http://localhost:3000";

                   await Task.CompletedTask;
               },
               OnAuthenticationFailed = context =>
               {
                   Debug.WriteLine("Authentication failed: " + context.Exception.Message);
                   return Task.CompletedTask;
               }
           };
       });

        return services;
    }

    public static IApplicationBuilder UseTokenHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<AuthenticationHeaderSubstitutionMiddleware>();
        return app;
    }
}
