using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Inventory;

public record GetInventoryQuery(string FarmId) : IRequest<List<InventoryItemDto>>;

public class GetInventoryQueryHandler : IRequestHandler<GetInventoryQuery, List<InventoryItemDto>>
{
    private readonly IInventoryRepository _inventoryRepo;

    public GetInventoryQueryHandler(IInventoryRepository inventoryRepo) => _inventoryRepo = inventoryRepo;

    public async Task<List<InventoryItemDto>> Handle(GetInventoryQuery query, CancellationToken ct)
    {
        var items = await _inventoryRepo.GetByFarmIdAsync(query.FarmId, ct);

        return items.Select(i => new InventoryItemDto(
            i.Id, i.FarmId, i.Name, i.Unit, i.Category,
            i.CurrentStock, i.ReorderThreshold, i.IsLowStock,
            i.ChemicalProductId, null, null)).ToList();
    }
}
