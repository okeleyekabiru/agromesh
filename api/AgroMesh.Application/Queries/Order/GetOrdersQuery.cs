using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Domain.Enums;
using MediatR;

namespace AgroMesh.Application.Queries.Order;

public record GetOrdersQuery(string FarmId, OrderStatus? Status) : IRequest<List<OrderDto>>;

public class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, List<OrderDto>>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public GetOrdersQueryHandler(IOrderRepository orderRepo, ISupplierRepository supplierRepo, IInventoryRepository inventoryRepo)
    {
        _orderRepo = orderRepo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<List<OrderDto>> Handle(GetOrdersQuery query, CancellationToken ct)
    {
        var orders = await _orderRepo.GetByFarmIdAsync(query.FarmId, query.Status, ct);

        // One round trip for every supplier/item on the farm, joined in memory — avoids an N+1
        // query per order in the list, same convention GetStockAdjustmentRequestsQueryHandler
        // already established for its own list query (see that handler's doc comment).
        var suppliers = await _supplierRepo.GetByFarmIdAsync(query.FarmId, ct);
        var suppliersById = suppliers.ToDictionary(s => s.Id);
        var items = await _inventoryRepo.GetByFarmIdAsync(query.FarmId, ct);
        var itemsById = items.ToDictionary(i => i.Id);

        return orders
            .Select(order => OrderDtoMapping.ToDto(order, suppliersById[order.SupplierId], itemsById))
            .ToList();
    }
}
