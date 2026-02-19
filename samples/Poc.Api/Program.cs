using AppForeach.TokenHandler.Extensions;
using AppForeach.TokenHandler.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Keycloak authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = builder.Configuration["Keycloak:Authority"];
        options.Audience = builder.Configuration["Keycloak:Audience"];
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true
        };
    });

builder.Services.Configure<OpenIdConnectOptions>("oidc", options =>
{
    options.Authority = builder.Configuration.GetValue<string>("Keycloak:Authority");

    options.ClientId = builder.Configuration.GetValue<string>("Keycloak:ClientId");
    options.ClientSecret = builder.Configuration.GetValue<string>("Keycloak:ClientSecret");
});

// Add authorization services
//builder.Services.AddAuthorization();

builder.Services.AddTokenExchangeDelegatingHandler();

// Configure HttpClient for InternalApi
builder.Services.AddHttpClient("InternalApi", client =>
{
    var baseUrl = builder.Configuration["InternalApi:BaseUrl"] ?? "http://localhost:5200";
    client.BaseAddress = new Uri(baseUrl);
}).AddHttpMessageHandler<TokenExchangeDelegatingHandler>(); ;

// Add controllers middleware
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Use controllers middleware
app.MapControllers();

app.Run();
