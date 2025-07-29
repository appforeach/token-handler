using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Poc.InternalApi.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize()]
public class InternalDataController : ControllerBase
{
    private static readonly string[] SampleData = new[]
    {
        "Internal-Item-1", "Internal-Item-2", "Internal-Item-3", "Internal-Item-4", "Internal-Item-5"
    };

    private readonly ILogger<InternalDataController> _logger;

    public InternalDataController(ILogger<InternalDataController> logger)
    {
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Get()
    {
        _logger.LogInformation("InternalData endpoint called");
        
        var result = Enumerable.Range(1, 5).Select(index => new InternalDataItem
        {
            Id = index,
            Name = SampleData[Random.Shared.Next(SampleData.Length)],
            CreatedAt = DateTime.Now.AddDays(-index),
            IsActive = index % 2 == 0
        })
        .ToArray();

        return Ok(result);
    }
}

public class InternalDataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}
