using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Inventory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/inventory-categories")]
public class InventoryCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryCategoriesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetInventoryCategoriesQuery(farmId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(string farmId, [FromBody] CreateInventoryCategoryRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateInventoryCategoryCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/inventory-categories/{result.Id}", result);
    }
}
