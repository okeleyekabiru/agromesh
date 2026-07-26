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
///
/// Who performed the pick is the authenticated caller (<see cref="ICurrentUserAccessor"/>), not a
/// client-chosen <see cref="Worker"/> — manually picking a Worker from a dropdown wasn't a useful
/// attribution (a worker is farm labor, not necessarily the logged-in office/company user actually
/// operating the app). The pre-existing <see cref="StockMovement.WorkerId"/> column is left in place
/// (harmless, nullable, simply unpopulated by this flow going forward) rather than a disruptive
/// drop-column migration, since nothing else reads it either. This endpoint requires
/// <see cref="ICurrentUserAccessor.UserId"/> to be resolvable, so the controller action gates it
/// with [Authorize] (unlike its sibling actions on InventoryController, which stay open).
/// </summary>
public record PickStockCommand(string FarmId, string InventoryItemId, PickStockRequest Request) : IRequest<PickStockResponse>;

public class PickStockCommandHandler : IRequestHandler<PickStockCommand, PickStockResponse>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ISectionRepository _sectionRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public PickStockCommandHandler(IInventoryRepository inventoryRepo, ISectionRepository sectionRepo, ICurrentUserAccessor currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _sectionRepo = sectionRepo;
        _currentUser = currentUser;
    }

    public async Task<PickStockResponse> Handle(PickStockCommand command, CancellationToken ct)
    {
        var req = command.Request;

        if (string.IsNullOrWhiteSpace(req.SectionId))
            throw new DomainException("A section must be selected for a stock pick.");
        if (string.IsNullOrWhiteSpace(req.Purpose))
            throw new DomainException("A purpose is required for a stock pick.");

        // Checked before any repository write — a request that can't be attributed to an
        // authenticated caller (should be unreachable once [Authorize] gates this action) must
        // never apply a stock change.
        var performedByUserId = _currentUser.UserId
            ?? throw new DomainException("Could not identify the current user.");

        var item = await _inventoryRepo.GetByIdAsync(command.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");
        // Defence in depth: the item id came from the client, so make sure it actually belongs to
        // the farm in the route rather than trusting it blindly (mirrors CreateInventoryItemCommand's
        // ResolveTargetItemAsync IDOR guard).
        if (item.FarmId != command.FarmId)
            throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");

        var section = await _sectionRepo.GetByIdWithBlocksAsync(req.SectionId, ct)
            ?? throw new DomainException($"Section '{req.SectionId}' not found.");
        // Same IDOR guard, applied to the section id.
        if (section.FarmId != command.FarmId)
            throw new DomainException($"Section '{req.SectionId}' not found.");

        if (string.IsNullOrWhiteSpace(req.BlockId))
        {
            // Server-side enforcement of "Block is required once the section has any" — the
            // frontend's canSubmitPick guard enforces this too, but a client can't be trusted
            // alone to enforce a data-integrity rule.
            if (section.Blocks.Count > 0)
                throw new DomainException($"Section '{req.SectionId}' has blocks — a block must be selected for a stock pick.");
        }
        else if (!section.Blocks.Any(b => b.Id == req.BlockId))
        {
            throw new DomainException($"Block '{req.BlockId}' does not belong to section '{req.SectionId}'.");
        }

        // No try/catch: insufficient stock or a non-positive quantity throws DomainException here
        // and propagates unhandled — this is the entire point of being a separate code path from
        // IInventoryDeductionService's catch-and-warn behavior.
        item.DeductStock(req.Quantity);
        await _inventoryRepo.UpdateAsync(item, ct);

        var movement = StockMovement.CreateOut(
            item.FarmId, item.Id, req.Quantity, notes: req.Purpose,
            performedByUserId: performedByUserId, sectionId: req.SectionId, blockId: req.BlockId);
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
