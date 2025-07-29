using AppForeach.TokenHandler.Extensions;

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

        builder.Services.AddTokenHandler(options =>
        {
            options.Authority = builder.Configuration.GetValue<string>("Keycloak:Authority");
            options.ClientId = builder.Configuration.GetValue<string>("Keycloak:ClientId");
            options.ClientSecret = builder.Configuration.GetValue<string>("Keycloak:ClientSecret");
            options.Realm = builder.Configuration.GetValue<string>("Keycloak:Realm");
        });

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

        app.UseTokenHandler();

        app.MapControllers();
        app.MapReverseProxy();

        app.Run();
    }
}