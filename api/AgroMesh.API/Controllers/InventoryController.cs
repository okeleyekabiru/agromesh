using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Inventory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/inventory")]
public class InventoryController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IInventoryRepository _inventoryRepo;

    public InventoryController(IMediator mediator, IInventoryRepository inventoryRepo)
    {
        _mediator = mediator;
        _inventoryRepo = inventoryRepo;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetInventoryQuery(farmId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(string farmId, [FromBody] CreateInventoryItemRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateInventoryItemCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/inventory/{result.Id}", result);
    }

    [HttpGet("low-stock")]
    public async Task<IActionResult> GetLowStock(string farmId, CancellationToken ct)
    {
        var items = await _inventoryRepo.GetLowStockAsync(farmId, ct);
        return Ok(items.Select(i => new LowStockAlertDto(i.Id, i.Name, i.CurrentStock, i.Unit, i.ReorderThreshold)));
    }

    [HttpGet("{itemId}/movements")]
    public async Task<IActionResult> GetMovements(string itemId, [FromQuery] int limit = 50, CancellationToken ct = default)
    {
        var movements = await _inventoryRepo.GetMovementsAsync(itemId, limit, ct);
        return Ok(movements.Select(m => new StockMovementDto(m.Id, m.InventoryItemId, m.Type, m.Quantity, m.ActivityLogId, m.TransactionId, m.Notes, m.Date)));
    }

    [HttpPatch("{itemId}/adjust")]
    public async Task<IActionResult> Adjust(string itemId, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        await _mediator.Send(new AdjustInventoryStockCommand(itemId, request.NewQuantity, request.Notes), ct);
        return NoContent();
    }
}
