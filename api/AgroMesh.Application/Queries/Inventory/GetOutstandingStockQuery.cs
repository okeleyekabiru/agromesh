using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Inventory;

/// <summary>Stock that has been picked out of the store but not yet used or returned.</summary>
public record GetOutstandingStockQuery(string FarmId) : IRequest<List<OutstandingStockDto>>;

public class GetOutstandingStockQueryHandler : IRequestHandler<GetOutstandingStockQuery, List<OutstandingStockDto>>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetOutstandingStockQueryHandler(IInventoryRepository inventoryRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<List<OutstandingStockDto>> Handle(GetOutstandingStockQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new NotFoundException("Farm", query.FarmId);
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", query.FarmId);

        var picks = await _inventoryRepo.GetOutstandingPicksAsync(query.FarmId, ct: ct);
        var items = (await _inventoryRepo.GetByFarmIdAsync(query.FarmId, ct)).ToDictionary(i => i.Id);

        return picks
            .Where(p => items.ContainsKey(p.InventoryItemId) && p.BlockId != null)
            .Select(p =>
            {
                var item = items[p.InventoryItemId];
                return new OutstandingStockDto(
                    p.Id, item.Id, item.Name, item.Unit, p.BlockId!,
                    p.Quantity, p.UsedQuantity, p.ReturnedQuantity, p.Outstanding,
                    p.Date, p.PerformedByUserId!);
            })
            .ToList();
    }
}
