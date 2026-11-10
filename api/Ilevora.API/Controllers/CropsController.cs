using Ilevora.Application.Commands.Crop;
using Ilevora.Application.DTOs;
using Ilevora.Application.Queries.Crop;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ilevora.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/crops")]
public class CropsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CropsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetCropsQuery(farmId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(string farmId, [FromBody] CreateCropRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateCropCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/crops/{result.Id}", result);
    }
}
