namespace AppForeach.TokenHandler.Extensions;

public class TokenHandlerOptions
{
    public string Authority { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Realm { get; set; } = string.Empty;
    public TimeSpan RefreshBeforeExpirationInMinutes { get; set; } = TimeSpan.FromMinutes(5);
    public string RedisConnectionString { get; set; } = "localhost:6379";

    public static TokenHandlerOptions Default => new TokenHandlerOptions
    {
        Authority = "http://localhost:8080/realms/poc",
        ClientId = "poc-api",
        ClientSecret = "your-client-secret-here",
        Realm = "poc",
        RefreshBeforeExpirationInMinutes = TimeSpan.FromMinutes(5),
        RedisConnectionString = "localhost:6379"
    };
}
