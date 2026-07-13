using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

/// <summary>Admin-only. Marks a Pending request Rejected — no item/stock changes at all.</summary>
public record RejectStockAdjustmentCommand(string FarmId, string RequestId, string? ReviewNotes) : IRequest<StockAdjustmentRequestDto>;

public class RejectStockAdjustmentCommandHandler : IRequestHandler<RejectStockAdjustmentCommand, StockAdjustmentRequestDto>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IStockAdjustmentRequestRepository _requestRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public RejectStockAdjustmentCommandHandler(
        IInventoryRepository inventoryRepo, IStockAdjustmentRequestRepository requestRepo, ICurrentUserAccessor currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _requestRepo = requestRepo;
        _currentUser = currentUser;
    }

    public async Task<StockAdjustmentRequestDto> Handle(RejectStockAdjustmentCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAdmin)
            throw new ForbiddenException("Only an admin can reject a stock adjustment request.");

        var request = await _requestRepo.GetByIdAsync(command.RequestId, ct)
            ?? throw new DomainException($"Stock adjustment request '{command.RequestId}' not found.");
        if (request.FarmId != command.FarmId)
            throw new DomainException($"Stock adjustment request '{command.RequestId}' not found.");

        var item = await _inventoryRepo.GetByIdAsync(request.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{request.InventoryItemId}' not found.");

        var reviewerId = _currentUser.UserId
            ?? throw new DomainException("Could not identify the reviewing user.");
        // No inventory repo call of any kind — a rejection never touches CurrentStock/movements.
        request.Reject(reviewerId, command.ReviewNotes);
        await _requestRepo.UpdateAsync(request, ct);

        return AdjustInventoryStockCommandHandler.ToDto(request, item);
    }
}
