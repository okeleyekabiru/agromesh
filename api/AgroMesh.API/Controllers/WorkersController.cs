using AgroMesh.Application.Commands.Worker;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Worker;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/workers")]
public class WorkersController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, [FromQuery] bool activeOnly = true, CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetWorkersQuery(farmId, activeOnly), ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Add(string farmId, [FromBody] AddWorkerRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddWorkerCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/workers/{result.Id}", result);
    }
}
