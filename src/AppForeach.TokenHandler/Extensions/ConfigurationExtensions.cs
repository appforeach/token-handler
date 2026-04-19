using AppForeach.TokenHandler.Controllers;
using AppForeach.TokenHandler.Middleware;
using AppForeach.TokenHandler.Services;
using AppForeach.TokenHandler.Services.Expiring_Sessions_Refresh;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System.Diagnostics;

namespace AppForeach.TokenHandler.Extensions;

public static class ConfigurationExtensions
{
    public static string AuthenticationCookieName = "session-id";
    public const string TokenExchangeHttpClientName = "TokenExchange";

    public static IServiceCollection AddTokenExchangeDelegatingHandler(this IServiceCollection services)
    {
        services.AddScoped<ITokenExchangeService, TokenExchangeService>();
        services.AddTransient<TokenExchangeDelegatingHandler>();
        services.AddHttpContextAccessor();
        return services;
    }

    public static IServiceCollection AddExpiringTokensRefreshInfrastructure(this IServiceCollection services, Action<TokenHandlerOptions>? overrideOptions)
    {
        var tokenHandlerOptions = TokenHandlerOptions.Default;

        overrideOptions?.Invoke(tokenHandlerOptions);

        services.Configure<TokenHandlerOptions>(options =>
        {
            options.Authority = tokenHandlerOptions.Authority;
            options.ClientId = tokenHandlerOptions.ClientId;
            options.ClientSecret = tokenHandlerOptions.ClientSecret;
            options.Realm = tokenHandlerOptions.Realm;
            options.RefreshBeforeExpirationInMinutes = tokenHandlerOptions.RefreshBeforeExpirationInMinutes;
        });

        services.AddMemoryCache();
        services.AddDistributedMemoryCache(); // For development. In production, use Redis or SQL Server
        services.AddHybridCache();
        services.AddHttpClient(TokenExchangeHttpClientName);
        services.TryAddSingleton<ITokenStorageService, TokenStorageService>();
        services.TryAddSingleton<ITokenRefreshService, TokenRefreshService>();
        services.TryAddSingleton<IExpiringTokensRefreshService, ExpiringTokensRefreshService>();

        return services;
    }

    public static IServiceCollection AddTokenHandler(this IServiceCollection services, Action<TokenHandlerOptions>? overrideOptions)
    {
        var tokenHandlerOptions = TokenHandlerOptions.Default;
        overrideOptions?.Invoke(tokenHandlerOptions);

        services.AddExpiringTokensRefreshInfrastructure(options =>
        {
            options.Authority = tokenHandlerOptions.Authority;
            options.ClientId = tokenHandlerOptions.ClientId;
            options.ClientSecret = tokenHandlerOptions.ClientSecret;
            options.Realm = tokenHandlerOptions.Realm;
            options.RefreshBeforeExpirationInMinutes = tokenHandlerOptions.RefreshBeforeExpirationInMinutes;
        });

        services.AddHttpContextAccessor();

        // Register token exchange service
        services.AddTransient<ITokenExchangeService, TokenExchangeService>();
        services.AddTransient<TokenExchangeDelegatingHandler>();

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
                   var tokensStorage = context.HttpContext.RequestServices.GetRequiredService<ITokenStorageService>();
                   Debug.WriteLine($"Authenticated user: {context.Principal?.Identity?.Name}");

                   if (context.TokenEndpointResponse is null)
                       return;

                   var sessionId = Guid.NewGuid().ToString();
                   await tokensStorage.StoreAsync(sessionId, context.TokenEndpointResponse);

                   var httpContext = context.HttpContext;
                   httpContext.Response.Cookies.Append(AuthenticationCookieName, sessionId, new CookieOptions
                   {
                       HttpOnly = true,
                       Secure = true,
                       // SameSite = SameSiteMode.Strict,
                       // Expires = DateTimeOffset.UtcNow.AddHours(1)
                   });

                   if (context.Properties is not null)
                   {
                       context.Properties.RedirectUri = context.ProtocolMessage.RedirectUri ?? "http://localhost:3000";
                   }

                   await Task.CompletedTask;
               },
               OnAuthenticationFailed = context =>
               {
                   Debug.WriteLine("Authentication failed: " + context.Exception.Message);
                   return Task.CompletedTask;
               }
           };
       });

        services.AddControllers()
          .AddApplicationPart(typeof(TokenHandlerController).Assembly);


        return services;
    }

    public static IApplicationBuilder UseTokenHandler(this IApplicationBuilder app)
    {
        app.UseMiddleware<AuthenticationHeaderSubstitutionMiddleware>();
        return app;
    }
}
