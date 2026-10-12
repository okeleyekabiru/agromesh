using AgroMesh.Application.Commands.Block;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Block;
using AgroMesh.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Authorize]
[Route("api/farms/{farmId}/blocks")]
public class BlocksController : ControllerBase
{
    private readonly IMediator _mediator;

    public BlocksController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetBlocksQuery(farmId), ct));

    /// <summary>Crop condition from satellite (NDVI and NDMI) for every block.</summary>
    [HttpGet("satellite")]
    public async Task<IActionResult> Satellite(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetSatelliteOverviewQuery(farmId), ct));

    /// <summary>The dates a block has satellite pictures for, and where to place them on the map.</summary>
    [HttpGet("{blockId}/satellite")]
    public async Task<IActionResult> SatelliteView(string farmId, string blockId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetBlockSatelliteViewQuery(farmId, blockId), ct));

    /// <summary>A satellite picture of the block (PNG) for one layer (TrueColour, Ndvi, Ndmi) and window start date (yyyy-MM-dd).</summary>
    [HttpGet("{blockId}/satellite/image")]
    public async Task<IActionResult> SatelliteImage(string farmId, string blockId, [FromQuery] string layer, [FromQuery] DateTime date, CancellationToken ct)
    {
        if (!Enum.TryParse<SatelliteLayer>(layer, ignoreCase: true, out var l) || !Enum.IsDefined(l))
            return BadRequest(new { detail = "layer must be TrueColour, Ndvi or Ndmi." });
        var png = await _mediator.Send(new GetBlockSatelliteImageQuery(farmId, blockId, l, date), ct);
        Response.Headers.CacheControl = "private, max-age=86400";
        return File(png, "image/png");
    }

    /// <summary>Fetches fresh satellite readings now (at most once an hour).</summary>
    [HttpPost("satellite/refresh")]
    public async Task<IActionResult> RefreshSatellite(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new RefreshSatelliteCommand(farmId), ct));

    /// <summary>Blocks with work part-done right now (done vs left, and each job's marked area).</summary>
    [HttpGet("coverage-progress")]
    public async Task<IActionResult> CoverageProgress(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetFarmCoverageProgressQuery(farmId), ct));

    /// <summary>What has been worked and what is left on a block, for one activity type (name or number) in its current cycle.</summary>
    [HttpGet("{blockId}/coverage")]
    public async Task<IActionResult> Coverage(string farmId, string blockId, [FromQuery] string activityType, CancellationToken ct)
    {
        if (!Enum.TryParse<ActivityType>(activityType, ignoreCase: true, out var type) || !Enum.IsDefined(type))
            return BadRequest(new { detail = "activityType must be an activity name such as Weeding, or its number." });
        return Ok(await _mediator.Send(new GetBlockCoverageQuery(farmId, blockId, type), ct));
    }

    /// <summary>Marks the current cycle of one kind of work finished by hand, when the marked areas fall just short of the whole block.</summary>
    [HttpPost("{blockId}/coverage/complete")]
    public async Task<IActionResult> CompleteCoverage(string farmId, string blockId, [FromQuery] string activityType, [FromBody] CompleteCoverageRequest? body, CancellationToken ct)
    {
        if (!Enum.TryParse<ActivityType>(activityType, ignoreCase: true, out var type) || !Enum.IsDefined(type))
            return BadRequest(new { detail = "activityType must be an activity name such as Weeding, or its number." });
        return Ok(await _mediator.Send(new CompleteCoverageCommand(farmId, blockId, type, body?.Reason), ct));
    }

    [HttpPost]
    public async Task<IActionResult> Create(string farmId, [FromBody] CreateBlockRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBlockCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/blocks/{result.Id}", result);
    }

    [HttpPatch("{blockId}")]
    public async Task<IActionResult> Update(string blockId, [FromBody] UpdateBlockRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new UpdateBlockCommand(blockId, request), ct));
}

public record CompleteCoverageRequest(string? Reason);
