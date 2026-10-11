using AgroMesh.Application.Commands.WorkerRole;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Worker;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/worker-roles")]
public class WorkerRolesController : ControllerBase
{
    private readonly IMediator _mediator;

    public WorkerRolesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetWorkerRolesQuery(farmId), ct));

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create(string farmId, [FromBody] CreateWorkerRoleRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateWorkerRoleCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/worker-roles/{result.Id}", result);
    }

    [Authorize]
    [HttpPatch("{roleId}/rate")]
    public async Task<IActionResult> UpdateRate(string roleId, [FromBody] UpdateWorkerRoleRateRequest request, CancellationToken ct)
    {
        await _mediator.Send(new UpdateWorkerRoleRateCommand(roleId, request.PayRate, request.PayFrequency), ct);
        return NoContent();
    }
}
