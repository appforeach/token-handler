using AppForeach.TokenHandler.BackgroundWorker;
using AppForeach.TokenHandler.Extensions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<ExpiringTokensRefreshWorkerOptions>(
    builder.Configuration.GetSection(ExpiringTokensRefreshWorkerOptions.SectionName));

builder.Services.AddExpiringTokensRefreshInfrastructure(options =>
{
    options.Authority = builder.Configuration.GetValue<string>("Keycloak:Authority") ?? TokenHandlerOptions.Default.Authority;
    options.ClientId = builder.Configuration.GetValue<string>("Keycloak:ClientId") ?? TokenHandlerOptions.Default.ClientId;
    options.ClientSecret = builder.Configuration.GetValue<string>("Keycloak:ClientSecret") ?? TokenHandlerOptions.Default.ClientSecret;
    options.Realm = builder.Configuration.GetValue<string>("Keycloak:Realm") ?? TokenHandlerOptions.Default.Realm;
    options.RefreshBeforeExpirationInMinutes = builder.Configuration.GetValue<TimeSpan?>("ExpiringTokensRefreshWorker:RefreshBeforeExpiration")
        ?? TokenHandlerOptions.Default.RefreshBeforeExpirationInMinutes;
    options.RedisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") ?? TokenHandlerOptions.Default.RedisConnectionString;
});

builder.Services.AddHostedService<ExpiringTokensRefreshWorker>();

var host = builder.Build();
host.Run();
