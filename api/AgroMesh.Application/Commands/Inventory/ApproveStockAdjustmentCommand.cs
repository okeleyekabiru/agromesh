using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

/// <summary>
/// Admin-only. Applies a Pending StockAdjustmentRequest via the existing, unmodified
/// InventoryItem.AdjustStock (no arithmetic duplicated here) and records the same
/// StockMovement.CreateAdjustment factory the pre-Phase-3 immediate-apply handler used.
/// </summary>
public record ApproveStockAdjustmentCommand(string FarmId, string RequestId, string? ReviewNotes) : IRequest<StockAdjustmentRequestDto>;

public class ApproveStockAdjustmentCommandHandler : IRequestHandler<ApproveStockAdjustmentCommand, StockAdjustmentRequestDto>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IStockAdjustmentRequestRepository _requestRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public ApproveStockAdjustmentCommandHandler(
        IInventoryRepository inventoryRepo, IStockAdjustmentRequestRepository requestRepo, ICurrentUserAccessor currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _requestRepo = requestRepo;
        _currentUser = currentUser;
    }

    public async Task<StockAdjustmentRequestDto> Handle(ApproveStockAdjustmentCommand command, CancellationToken ct)
    {
        // Role check first — a non-admin gets a 403 regardless of whether the request exists, so
        // this endpoint doesn't leak request existence to an unauthorized caller.
        if (!_currentUser.IsAdmin)
            throw new ForbiddenException("Only an admin can approve a stock adjustment request.");

        var request = await _requestRepo.GetByIdAsync(command.RequestId, ct)
            ?? throw new DomainException($"Stock adjustment request '{command.RequestId}' not found.");
        if (request.FarmId != command.FarmId)
            throw new DomainException($"Stock adjustment request '{command.RequestId}' not found.");
        // Checked BEFORE any item mutation below — re-approving an already-reviewed request must
        // never double-apply the stock change. request.Approve(...) re-validates this too (defence
        // in depth for the entity's own invariant), but by then it would already be too late to
        // stop the item side effects if this check weren't here first.
        if (request.Status != StockAdjustmentStatus.Pending)
            throw new DomainException($"This stock adjustment request has already been {request.Status.ToString().ToLowerInvariant()}.");

        var item = await _inventoryRepo.GetByIdAsync(request.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{request.InventoryItemId}' not found.");

        // Delta must be computed BEFORE AdjustStock overwrites CurrentStock, or every movement
        // would record a zero quantity.
        var delta = request.RequestedQuantity - item.CurrentStock;
        item.AdjustStock(request.RequestedQuantity, request.Notes);
        await _inventoryRepo.UpdateAsync(item, ct);

        var movement = StockMovement.CreateAdjustment(item.FarmId, item.Id, delta, request.Notes);
        await _inventoryRepo.AddMovementAsync(movement, ct);

        var reviewerId = _currentUser.UserId
            ?? throw new DomainException("Could not identify the reviewing user.");
        request.Approve(reviewerId, command.ReviewNotes);
        await _requestRepo.UpdateAsync(request, ct);

        return AdjustInventoryStockCommandHandler.ToDto(request, item);
    }
}
