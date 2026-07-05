using AgroMesh.Application.Commands.Farm;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Farm;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Authorize]
[Route("api/farms")]
public class FarmsController : ControllerBase
{
    private readonly IMediator _mediator;

    public FarmsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetFarmsQuery(), ct));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateFarmRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateFarmCommand(request), ct);
        return CreatedAtAction(nameof(GetById), new { farmId = result.Id }, result);
    }

    [HttpGet("{farmId}")]
    public async Task<IActionResult> GetById(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetFarmQuery(farmId), ct));

    [HttpPatch("{farmId}")]
    public async Task<IActionResult> UpdateProfile(string farmId, [FromBody] UpdateFarmProfileRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new UpdateFarmProfileCommand(farmId, request), ct));

    [HttpPatch("{farmId}/location")]
    public async Task<IActionResult> SetLocation(string farmId, [FromBody] SetFarmLocationRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new SetFarmLocationCommand(farmId, request), ct));

    [HttpPatch("{farmId}/setup-status")]
    public async Task<IActionResult> SetSetupStatus(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new SetFarmSetupStatusCommand(farmId), ct));

    [HttpGet("{farmId}/onboarding-status")]
    public async Task<IActionResult> GetOnboardingStatus(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetOnboardingStatusQuery(farmId), ct));

    [HttpPost("{farmId}/allocation-rules")]
    public async Task<IActionResult> SetAllocationRules(string farmId, [FromBody] List<AllocationRuleInput> rules, CancellationToken ct)
    {
        await _mediator.Send(new SetAllocationRulesCommand(farmId, rules), ct);
        return NoContent();
    }
}
