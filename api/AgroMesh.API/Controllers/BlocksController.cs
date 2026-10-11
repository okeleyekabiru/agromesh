using AgroMesh.Application.Commands.Block;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Block;
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
