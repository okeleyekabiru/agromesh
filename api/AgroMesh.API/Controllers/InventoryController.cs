using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Inventory;
using MediatR;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPut("{itemId}/pricing")]
    [Authorize]
    public async Task<IActionResult> SetPricing(string farmId, string itemId, [FromBody] SetItemPricingRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new SetItemPricingCommand(farmId, itemId, request), ct));

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
        return Ok(movements.Select(m => new StockMovementDto(m.Id, m.InventoryItemId, m.Type, m.Quantity, m.ActivityLogId, m.TransactionId, m.OrderId, m.Notes, m.Date)));
    }

    // Requires [Authorize]: unlike the other actions on this controller (a pre-existing,
    // out-of-scope gap — see docs/features/inventory-adjustment-approval/spec.md), this one needs
    // to know who's calling to record RequestedByUserId.
    [Authorize]
    [HttpPatch("{itemId}/adjust")]
    public async Task<IActionResult> Adjust(string farmId, string itemId, [FromBody] AdjustStockRequest request, CancellationToken ct)
    {
        // Submitting now creates a new sub-resource (a pending request) rather than mutating the
        // item in place — 201, not the old 204 NoContent.
        var result = await _mediator.Send(new AdjustInventoryStockCommand(farmId, itemId, request.NewQuantity, request.Notes), ct);
        return Created($"api/farms/{farmId}/inventory-adjustments/{result.Id}", result);
    }

    // Stock picked out of the store that hasn't yet been used in an activity or returned.
    [Authorize]
    [HttpGet("outstanding")]
    public async Task<IActionResult> GetOutstanding(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetOutstandingStockQuery(farmId), ct));

    // Leftover stock handed back to the store (e.g. carried 50 kg, used 38, returning 12).
    [Authorize]
    [HttpPost("{itemId}/return")]
    public async Task<IActionResult> Return(string farmId, string itemId, [FromBody] ReturnStockRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new ReturnStockCommand(farmId, itemId, request), ct));

    // Requires [Authorize]: PickStockCommandHandler now auto-attributes the authenticated caller
    // (ICurrentUserAccessor.UserId) instead of trusting a client-chosen WorkerId — same reasoning
    // as Adjust above, and the same scoped-not-controller-wide gate this codebase already uses
    // (the other actions on this controller stay open, a pre-existing, out-of-scope gap).
    [Authorize]
    [HttpPost("{itemId}/pick")]
    public async Task<IActionResult> Pick(string farmId, string itemId, [FromBody] PickStockRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new PickStockCommand(farmId, itemId, request), ct));
}
