using Poc.Yarp.Token_Handler.Middleware;

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
