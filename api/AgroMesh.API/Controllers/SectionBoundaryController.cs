using AgroMesh.Application.Commands.Section;
using AgroMesh.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Authorize]
[Route("api/sections/{sectionId}/boundary")]
public class SectionBoundaryController : ControllerBase
{
    private readonly IMediator _mediator;

    public SectionBoundaryController(IMediator mediator) => _mediator = mediator;

    [HttpPost("points")]
    public async Task<IActionResult> SubmitPoints(string sectionId, [FromBody] SubmitBoundaryPointsRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new SubmitBoundaryPointsCommand(sectionId, request), ct));

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(string sectionId, [FromBody] UploadBoundaryFileRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new UploadBoundaryFileCommand(sectionId, request), ct));
}
