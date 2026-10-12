using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using MediatR;

namespace AgroMesh.Application.Queries.Inventory;

/// <summary>Price changes for a farm: every status when <see cref="Status"/> is null; the approvals queue asks for Pending.</summary>
public record GetPriceChangeRequestsQuery(string FarmId, PriceChangeStatus? Status) : IRequest<List<PriceChangeRequestDto>>;

public class GetPriceChangeRequestsQueryHandler : IRequestHandler<GetPriceChangeRequestsQuery, List<PriceChangeRequestDto>>
{
    private readonly IPriceChangeRequestRepository _requestRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public GetPriceChangeRequestsQueryHandler(IPriceChangeRequestRepository requestRepo, IInventoryRepository inventoryRepo)
    {
        _requestRepo = requestRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<List<PriceChangeRequestDto>> Handle(GetPriceChangeRequestsQuery query, CancellationToken ct)
    {
        var requests = await _requestRepo.GetByFarmIdAsync(query.FarmId, query.Status, ct);
        var items = (await _inventoryRepo.GetByFarmIdAsync(query.FarmId, ct)).ToDictionary(i => i.Id);

        return requests
            .Select(r => Commands.Inventory.PriceChangeMapping.ToDto(r, items.GetValueOrDefault(r.InventoryItemId)))
            .ToList();
    }
}
