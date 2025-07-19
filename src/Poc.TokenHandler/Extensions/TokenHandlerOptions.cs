namespace Poc.TokenHandler.Extensions;
public class TokenHandlerOptions
{
    public string Authority { get; set; }
    public string ClientId { get; set; }
    public string ClientSecret { get; set; }
    public string Realm { get; set; }

    public static TokenHandlerOptions Default => new TokenHandlerOptions
    {
        Authority = "http://localhost:8080/realms/poc",
        ClientId = "poc-api",
        ClientSecret = "2ISb8zFHUU4Q5XZDd2xRN4LpkjMPz2mK",
        Realm = "poc"
    };
}
