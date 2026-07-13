using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

/// <summary>
/// The manual "pick / issue from stock" command — a farm worker physically takes stock for a
/// purpose that isn't (or isn't yet) tied to a logged Activity. This is a genuinely separate code
/// path from <see cref="IInventoryDeductionService"/> (the activity-log-driven auto-deduction): it
/// never calls that service, and unlike it, never catches the <see cref="DomainException"/> that
/// <see cref="InventoryItem.DeductStock"/> throws on insufficient stock — that exception is left to
/// propagate to the existing ExceptionMiddleware (→ HTTP 422), so a pick either fully applies or is
/// fully rejected. CurrentStock only ever changes through InventoryItem.DeductStock — no arithmetic
/// is duplicated here.
/// </summary>
public record PickStockCommand(string FarmId, string InventoryItemId, PickStockRequest Request) : IRequest<PickStockResponse>;

public class PickStockCommandHandler : IRequestHandler<PickStockCommand, PickStockResponse>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IWorkerRepository _workerRepo;

    public PickStockCommandHandler(IInventoryRepository inventoryRepo, IWorkerRepository workerRepo)
    {
        _inventoryRepo = inventoryRepo;
        _workerRepo = workerRepo;
    }

    public async Task<PickStockResponse> Handle(PickStockCommand command, CancellationToken ct)
    {
        var req = command.Request;

        if (string.IsNullOrWhiteSpace(req.WorkerId))
            throw new DomainException("A worker must be selected for a stock pick.");
        if (string.IsNullOrWhiteSpace(req.Purpose))
            throw new DomainException("A purpose is required for a stock pick.");

        var item = await _inventoryRepo.GetByIdAsync(command.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");
        // Defence in depth: the item id came from the client, so make sure it actually belongs to
        // the farm in the route rather than trusting it blindly (mirrors CreateInventoryItemCommand's
        // ResolveTargetItemAsync IDOR guard).
        if (item.FarmId != command.FarmId)
            throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");

        var worker = await _workerRepo.GetByIdAsync(req.WorkerId, ct)
            ?? throw new DomainException($"Worker '{req.WorkerId}' not found.");
        // Same IDOR guard, applied to the worker id — a picker can't be attributed to a worker from
        // another farm.
        if (worker.FarmId != command.FarmId)
            throw new DomainException($"Worker '{req.WorkerId}' not found.");

        // No try/catch: insufficient stock or a non-positive quantity throws DomainException here
        // and propagates unhandled — this is the entire point of being a separate code path from
        // IInventoryDeductionService's catch-and-warn behavior.
        item.DeductStock(req.Quantity);
        await _inventoryRepo.UpdateAsync(item, ct);

        var movement = StockMovement.CreateOut(item.FarmId, item.Id, req.Quantity, notes: req.Purpose, workerId: worker.Id);
        await _inventoryRepo.AddMovementAsync(movement, ct);

        // A pick that lands the stock to/below the reorder threshold is a warning, not a block — the
        // deduction above already succeeded by the time we get here.
        string? lowStockWarning = item.IsLowStock
            ? $"{item.Name} is running low ({item.CurrentStock} {item.Unit} remaining — reorder threshold: {item.ReorderThreshold})"
            : null;

        var dto = new InventoryItemDto(item.Id, item.FarmId, item.Name, item.Unit, item.CategoryId, item.Category.Name,
            item.CurrentStock, item.ReorderThreshold, item.IsLowStock, item.ChemicalProductId, null, movement.Date);

        return new PickStockResponse(dto, lowStockWarning);
    }
}
