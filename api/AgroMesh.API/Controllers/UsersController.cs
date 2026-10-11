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

    [HttpPost]
    public async Task<IActionResult> Add([FromBody] AddStaffUserRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddStaffUserCommand(request), ct);
        return StatusCode(201, result);
    }
}
