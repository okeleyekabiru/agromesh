using AgroMesh.Application.Commands.Activity;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Activity;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/activities")]
public class ActivitiesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ActivitiesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetActivitiesQuery(farmId, page, pageSize), ct));

    [HttpPost]
    public async Task<IActionResult> Log(string farmId, [FromBody] LogActivityRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LogActivityCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/activities/{result.Activity.Id}", result);
    }
}
