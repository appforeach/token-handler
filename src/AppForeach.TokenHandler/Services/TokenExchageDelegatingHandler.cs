using Microsoft.AspNetCore.Http;
using System.Net.Http.Headers;

namespace AppForeach.TokenHandler.Services;
public class TokenExchangeDelegatingHandler : DelegatingHandler
{
    private readonly ITokenExchangeService _tokenExchangeService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenExchangeDelegatingHandler(ITokenExchangeService tokenExchangeService, IHttpContextAccessor httpContextAccessor)
    {
        _tokenExchangeService = tokenExchangeService;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var incomingToken = _httpContextAccessor.HttpContext?.Request.Headers.Authorization
           .ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase);

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