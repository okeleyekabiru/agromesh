using AgroMesh.Application.Commands.Block;
using AgroMesh.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Authorize]
[Route("api/blocks/{blockId}/boundary")]
public class BlockBoundaryController : ControllerBase
{
    private readonly IMediator _mediator;

    public BlockBoundaryController(IMediator mediator) => _mediator = mediator;

    [HttpPost("points")]
    public async Task<IActionResult> SubmitPoints(string blockId, [FromBody] SubmitBoundaryPointsRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new SubmitBoundaryPointsCommand(blockId, request), ct));

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(string blockId, [FromBody] UploadBoundaryFileRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new UploadBoundaryFileCommand(blockId, request), ct));
}
