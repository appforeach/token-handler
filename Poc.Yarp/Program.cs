using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Poc.Yarp.Token_Handler.Middleware;
using System.Diagnostics;

internal class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        builder.Services.AddControllers();
        builder.Services.AddHttpClient();
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));
        builder.Services.AddSession();


        // Add these lines to your Program.cs or Startup.cs ConfigureServices method
        builder.Services.AddMemoryCache();
        builder.Services.AddDistributedMemoryCache(); // For development. In production, use Redis or SQL Server
        builder.Services.AddHybridCache();

        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(
                policy =>
                {
                    if (builder.Environment.IsDevelopment())
                    {
                        policy
                            .SetIsOriginAllowed(_ => true) // Allow any origin in development
                            .AllowAnyHeader()
                            .AllowAnyMethod()
                            .AllowCredentials();
                    }
                });
        });

        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "oidc";
        })
        .AddCookie(options => {
            options.LoginPath = "/Account/Login/";
        })
       .AddOpenIdConnect("oidc", options =>
        {
            options.Authority = "http://localhost:8080/realms/poc";
            options.ClientId = "poc-api";
            options.ClientSecret = "2ISb8zFHUU4Q5XZDd2xRN4LpkjMPz2mK";
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

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseDeveloperExceptionPage();
        }


        app.UseCors();
        app.UseSession();

        app.UseHttpsRedirection();
        app.UseRouting();

        app.UseMiddleware<AuthenticationHeaderSubstitutionMiddleware>();

        app.MapControllers();
        app.MapReverseProxy();

        app.Run();
    }
}