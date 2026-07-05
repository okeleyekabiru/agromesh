using AgroMesh.Application.Commands.Section;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Section;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Authorize]
[Route("api/farms/{farmId}/sections")]
public class SectionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SectionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetSectionsQuery(farmId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(string farmId, [FromBody] CreateSectionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSectionCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/sections/{result.Id}", result);
    }

    [HttpPost("{sectionId}/blocks")]
    public async Task<IActionResult> CreateBlock(string sectionId, [FromBody] CreateBlockRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateBlockCommand(sectionId, request), ct);
        return Created($"", result);
    }
}
