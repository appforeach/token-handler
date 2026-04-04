using Poc.BackgroundWorker.Services;

namespace Poc.BackgroundWorker.Workers;

/// <summary>
/// Background service that periodically calls the Weather API via YARP using client credentials authentication.
/// Demonstrates machine-to-machine authentication for background workers.
/// </summary>
public class WeatherBackgroundService : BackgroundService
{
    private readonly IClientCredentialsTokenService _tokenService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WeatherBackgroundService> _logger;

    public WeatherBackgroundService(
        IClientCredentialsTokenService tokenService,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<WeatherBackgroundService> logger)
    {
        _tokenService = tokenService;
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Weather Background Service starting...");

        var pollingInterval = _configuration.GetValue<int>("BackgroundWorker:PollingIntervalSeconds", 60);
        var apiAudience = _configuration.GetValue<string>("BackgroundWorker:ApiAudience");
        var baseUrl = _configuration.GetValue<string>("YarpApi:BaseUrl");

        _logger.LogInformation(
            "Configuration: Polling every {PollingInterval}s, API: {BaseUrl}, Audience: {Audience}",
            pollingInterval, baseUrl, apiAudience);

        // Wait a bit before starting to ensure services are ready
        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await FetchWeatherDataAsync(baseUrl!, apiAudience, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in weather background service iteration");
            }

            // Wait for the configured interval before next iteration
            _logger.LogDebug("Waiting {PollingInterval} seconds before next poll...", pollingInterval);
            await Task.Delay(TimeSpan.FromSeconds(pollingInterval), cancellationToken);
        }

        _logger.LogInformation("Weather Background Service stopping...");
    }

    private async Task FetchWeatherDataAsync(
        string baseUrl,
        string? apiAudience,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("=== Starting Weather API Call ===");

        // Step 1: Get a token using client credentials
        _logger.LogInformation("Step 1: Requesting access token for audience '{Audience}'...", apiAudience);
        
        var tokenResult = await _tokenService.GetAccessTokenAsync(
            audience: apiAudience,
            cancellationToken: cancellationToken);

        if (!tokenResult.IsSuccess)
        {
            _logger.LogError(
                "Failed to acquire access token: {Error} - {Description}",
                tokenResult.Error,
                tokenResult.ErrorDescription);
            return;
        }

        _logger.LogInformation(
            "? Access token acquired successfully (type: {TokenType}, expires in: {ExpiresIn}s)",
            tokenResult.TokenType,
            tokenResult.ExpiresIn);

        // Step 2: Call the Weather API via YARP with the Bearer token
        _logger.LogInformation("Step 2: Calling Weather API at {BaseUrl}/weatherforecast...", baseUrl);

        var httpClient = _httpClientFactory.CreateClient();
        httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenResult.AccessToken);

        var weatherUrl = $"{baseUrl}/weatherforecast";
        var response = await httpClient.GetAsync(weatherUrl, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            
            _logger.LogInformation(
                "? Weather API call successful (Status: {StatusCode})",
                response.StatusCode);
            
            _logger.LogDebug("Response: {Content}", content);
            
            // Try to parse and display weather data
            try
            {
                var weatherData = System.Text.Json.JsonSerializer.Deserialize<WeatherForecast[]>(content);
                if (weatherData != null && weatherData.Length > 0)
                {
                    _logger.LogInformation(
                        "Weather forecast received: {Count} entries, first entry: {Date} - {Temp}°F ({Summary})",
                        weatherData.Length,
                        weatherData[0].Date,
                        weatherData[0].TemperatureF,
                        weatherData[0].Summary);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not parse weather data");
            }
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "? Weather API call failed (Status: {StatusCode}): {ErrorContent}",
                response.StatusCode,
                errorContent);
        }

        _logger.LogInformation("=== Weather API Call Completed ===");
    }

    // Weather forecast model matching the API response
    private record WeatherForecast(
        DateOnly Date,
        int TemperatureC,
        int TemperatureF,
        string? Summary);
}
