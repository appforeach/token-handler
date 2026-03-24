using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Poc.BackgroundWorker.Services;
using Poc.BackgroundWorker.Workers;

var builder = Host.CreateApplicationBuilder(args);

// Configure OpenIdConnect options (reusing same configuration as TokenHandler)
builder.Services.Configure<OpenIdConnectOptions>("oidc", options =>
{
    options.Authority = builder.Configuration.GetValue<string>("Keycloak:Authority");
    options.ClientId = builder.Configuration.GetValue<string>("Keycloak:ClientId");
    options.ClientSecret = builder.Configuration.GetValue<string>("Keycloak:ClientSecret");
    
    // Note: These settings are not used for client credentials but kept for consistency
    options.RequireHttpsMetadata = false; // Development only
});

// Register HTTP client for client credentials
builder.Services.AddHttpClient("ClientCredentials");

// Register the client credentials token service
builder.Services.AddSingleton<IClientCredentialsTokenService, ClientCredentialsTokenService>();

// Register the weather background service
builder.Services.AddHostedService<WeatherBackgroundService>();

var host = builder.Build();
host.Run();
