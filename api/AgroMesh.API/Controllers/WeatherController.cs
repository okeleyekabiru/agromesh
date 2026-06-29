using AgroMesh.Application.Queries.Farm;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/weather")]
public class WeatherController : ControllerBase
{
    private readonly IMediator _mediator;

    public WeatherController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(string farmId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWeatherQuery(farmId), ct);
        if (result == null) return NotFound(new { detail = "Farm has no coordinates set or weather data unavailable." });
        return Ok(result);
    }
}
