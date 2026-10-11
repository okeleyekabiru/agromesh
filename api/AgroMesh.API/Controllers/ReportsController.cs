using AgroMesh.Application.Queries.Report;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/reports")]
public class ReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ReportsController(IMediator mediator) => _mediator = mediator;

    /// <summary>One month's costs; defaults to the current month.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(string farmId, [FromQuery] int? year, [FromQuery] int? month, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetReportQuery(farmId, year, month), ct));
}
