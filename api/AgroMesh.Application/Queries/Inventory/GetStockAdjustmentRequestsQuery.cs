using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using MediatR;

namespace AgroMesh.Application.Queries.Inventory;

/// <summary>
/// Lists StockAdjustmentRequests for a farm. Returns every status when <see cref="Status"/> is
/// null (simple, predictable REST semantics — no hidden server-side default); the approvals-queue
/// view is what explicitly requests Pending only.
/// </summary>
public record GetStockAdjustmentRequestsQuery(string FarmId, StockAdjustmentStatus? Status) : IRequest<List<StockAdjustmentRequestDto>>;

public class GetStockAdjustmentRequestsQueryHandler : IRequestHandler<GetStockAdjustmentRequestsQuery, List<StockAdjustmentRequestDto>>
{
    private readonly IStockAdjustmentRequestRepository _requestRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public GetStockAdjustmentRequestsQueryHandler(IStockAdjustmentRequestRepository requestRepo, IInventoryRepository inventoryRepo)
    {
        _requestRepo = requestRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<List<StockAdjustmentRequestDto>> Handle(GetStockAdjustmentRequestsQuery query, CancellationToken ct)
    {
        var requests = await _requestRepo.GetByFarmIdAsync(query.FarmId, query.Status, ct);

        // One round trip for every item on the farm, joined in memory — avoids an N+1 query per
        // request in the list (see spec.md Design decision 7).
        var items = await _inventoryRepo.GetByFarmIdAsync(query.FarmId, ct);
        var itemsById = items.ToDictionary(i => i.Id);

        return requests
            .Select(r => itemsById.TryGetValue(r.InventoryItemId, out var item)
                ? new StockAdjustmentRequestDto(
                    r.Id, r.FarmId, r.InventoryItemId, item.Name, item.Unit, item.CurrentStock,
                    r.RequestedQuantity, r.Status.ToString(), r.Notes, r.RequestedByUserId,
                    r.ReviewedByUserId, r.ReviewedAt, r.ReviewNotes, r.CreatedAt)
                : new StockAdjustmentRequestDto(
                    r.Id, r.FarmId, r.InventoryItemId, "(deleted item)", "", 0,
                    r.RequestedQuantity, r.Status.ToString(), r.Notes, r.RequestedByUserId,
                    r.ReviewedByUserId, r.ReviewedAt, r.ReviewNotes, r.CreatedAt))
            .OrderByDescending(r => r.CreatedAt)
            .ToList();
    }
}
