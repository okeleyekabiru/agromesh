using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Mapping;
using Ilevora.Domain.Exceptions;
using MediatR;

namespace Ilevora.Application.Queries.Order;

public record GetOrderByIdQuery(string FarmId, string OrderId) : IRequest<OrderDto>;

public class GetOrderByIdQueryHandler : IRequestHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public GetOrderByIdQueryHandler(IOrderRepository orderRepo, ISupplierRepository supplierRepo, IInventoryRepository inventoryRepo)
    {
        _orderRepo = orderRepo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<OrderDto> Handle(GetOrderByIdQuery query, CancellationToken ct)
    {
        var order = await _orderRepo.GetByIdAsync(query.OrderId, ct)
            ?? throw new DomainException($"Order '{query.OrderId}' not found.");
        OrderDtoMapping.EnsureFarmScoped(order, query.FarmId);

        var supplier = await OrderDtoMapping.ResolveSupplierAsync(_supplierRepo, query.FarmId, order.SupplierId, ct);
        var itemsById = await OrderDtoMapping.ResolveLineItemsAsync(_inventoryRepo, query.FarmId, order.LineItems, ct);
        return OrderDtoMapping.ToDto(order, supplier, itemsById);
    }
}
