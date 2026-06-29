using AgroMesh.Application.Queries.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/dashboard")]
public class DashboardController : ControllerBase
{
    private readonly IMediator _mediator;

    public DashboardController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetDashboardQuery(farmId), ct));
}
