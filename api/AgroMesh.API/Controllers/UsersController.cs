using AgroMesh.Application.Commands.Staff;
using AgroMesh.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly IMediator _mediator;

    public UsersController(IMediator mediator) => _mediator = mediator;

    /// <summary>The company's team (admin only).</summary>
    [HttpGet]
    public async Task<IActionResult> Team(CancellationToken ct) => Ok(await _mediator.Send(new GetTeamQuery(), ct));

    /// <summary>Farm managers by name, for the Supervisor choice when logging an activity. Any signed-in user.</summary>
    [HttpGet("managers")]
    public async Task<IActionResult> Managers(CancellationToken ct) => Ok(await _mediator.Send(new GetManagerOptionsQuery(), ct));

    /// <summary>Disable or re-enable a team member (admin only).</summary>
    [HttpPatch("{userId}/status")]
    public async Task<IActionResult> SetStatus(string userId, [FromBody] SetUserActiveRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new SetUserActiveCommand(userId, request.IsActive), ct));

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddStaffUserRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddStaffUserCommand(request), ct);
        return StatusCode(201, result);
    }
}
