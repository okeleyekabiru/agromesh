using AgroMesh.Application.Queries.Reference;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/reference")]
public class ReferenceController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReferenceController(IMediator mediator) => _mediator = mediator;

    [HttpGet("states")]
    public async Task<IActionResult> GetStates(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetStatesQuery(), ct));
}
