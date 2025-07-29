using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Poc.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class InternalDataProxyController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<InternalDataProxyController> _logger;

    public InternalDataProxyController(IHttpClientFactory httpClientFactory, ILogger<InternalDataProxyController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        using var httpClient = _httpClientFactory.CreateClient("InternalApi");
        _logger.LogInformation("Proxying request to InternalData API");

        try
        {
            var response = await httpClient.GetAsync("InternalData");

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
