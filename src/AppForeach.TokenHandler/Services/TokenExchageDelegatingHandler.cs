using System.Net.Http.Headers;

namespace AppForeach.TokenHandler.Services;
public class TokenExchangeDelegatingHandler : DelegatingHandler
{
    private readonly ITokenExchangeService _tokenExchangeService;

    public TokenExchangeDelegatingHandler(ITokenExchangeService tokenExchangeService)
    {
        _tokenExchangeService = tokenExchangeService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var incomingToken = request.Headers.Authorization?.Parameter;

        if (!string.IsNullOrEmpty(incomingToken) && request.RequestUri is not null)
        {
            var exchangedToken = await _tokenExchangeService.ExchangeForResourceAsync(
                incomingToken,
                request.RequestUri.ToString(),
                null,
                cancellationToken);

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", exchangedToken.AccessToken);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}