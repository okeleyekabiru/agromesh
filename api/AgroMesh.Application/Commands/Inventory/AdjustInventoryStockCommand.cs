using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

/// <summary>
/// Phase 3: submitting an adjustment no longer touches CurrentStock — it creates a Pending
/// StockAdjustmentRequest. CurrentStock only changes once an admin approves it
/// (ApproveStockAdjustmentCommand), which is the sole caller of InventoryItem.AdjustStock — no
/// arithmetic is duplicated here.
/// </summary>
public record AdjustInventoryStockCommand(string FarmId, string InventoryItemId, decimal NewQuantity, string? Notes) : IRequest<StockAdjustmentRequestDto>;

public class AdjustInventoryStockCommandHandler : IRequestHandler<AdjustInventoryStockCommand, StockAdjustmentRequestDto>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IStockAdjustmentRequestRepository _requestRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public AdjustInventoryStockCommandHandler(
        IInventoryRepository inventoryRepo, IStockAdjustmentRequestRepository requestRepo, ICurrentUserAccessor currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _requestRepo = requestRepo;
        _currentUser = currentUser;
    }

    public async Task<StockAdjustmentRequestDto> Handle(AdjustInventoryStockCommand command, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(command.Notes))
            throw new DomainException("A reason is required for a stock adjustment request.");

        var item = await _inventoryRepo.GetByIdAsync(command.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");
        // Defence in depth: the item id came from the client — make sure it actually belongs to the
        // farm in the route rather than trusting it blindly (mirrors CreateInventoryItemCommand's
        // ResolveTargetItemAsync / PickStockCommand's IDOR guard). The pre-Phase-3 handler had no
        // such check at all.
        if (item.FarmId != command.FarmId)
            throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");

        var requestedByUserId = _currentUser.UserId
            ?? throw new DomainException("Could not identify the requesting user.");

        var request = StockAdjustmentRequest.Create(command.FarmId, item.Id, command.NewQuantity, command.Notes, requestedByUserId);
        await _requestRepo.AddAsync(request, ct);

        return ToDto(request, item);
    }

    internal static StockAdjustmentRequestDto ToDto(StockAdjustmentRequest request, InventoryItem item) => new(
        request.Id, request.FarmId, request.InventoryItemId, item.Name, item.Unit, item.CurrentStock,
        request.RequestedQuantity, request.Status.ToString(), request.Notes, request.RequestedByUserId,
        request.ReviewedByUserId, request.ReviewedAt, request.ReviewNotes, request.CreatedAt);
}
