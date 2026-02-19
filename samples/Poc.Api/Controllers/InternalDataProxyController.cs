using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Poc.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class InternalDataProxyController : ControllerBase
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<InternalDataProxyController> _logger;

    public InternalDataProxyController(IHttpClientFactory httpClientFactory, ILogger<InternalDataProxyController> logger)
    {
        _httpClient = httpClientFactory.CreateClient("InternalApi");
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        _logger.LogInformation("Proxying request to InternalData API");

        try
        {
            // Forward the authorization header to the internal API
            if (Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    System.Net.Http.Headers.AuthenticationHeaderValue.Parse(authHeader.ToString());
            }

            var response = await _httpClient.GetAsync("InternalData");
            
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return Content(content, "application/json");
            }

            _logger.LogWarning("InternalData API returned status code: {StatusCode}", response.StatusCode);
            return StatusCode((int)response.StatusCode, await response.Content.ReadAsStringAsync());
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Error calling InternalData API");
            return StatusCode(503, "Internal API is unavailable");
        }
    }
}
