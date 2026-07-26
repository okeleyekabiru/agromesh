using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Domain.ValueObjects;
using MediatR;

namespace AgroMesh.Application.Commands.Order;

public record CreateOrderCommand(string FarmId, CreateOrderRequest Request) : IRequest<OrderDto>;

public class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public CreateOrderCommandHandler(IOrderRepository orderRepo, ISupplierRepository supplierRepo, IInventoryRepository inventoryRepo)
    {
        _orderRepo = orderRepo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<OrderDto> Handle(CreateOrderCommand command, CancellationToken ct)
    {
        var req = command.Request;

        // Resolve/validate BEFORE constructing the order — an unknown supplier or a line item that
        // doesn't resolve to a real, farm-scoped InventoryItem must fail before anything is persisted.
        var supplier = await OrderDtoMapping.ResolveSupplierAsync(_supplierRepo, command.FarmId, req.SupplierId, ct);
        var lineItems = req.LineItems.Select(l => new OrderLineItem(l.InventoryItemId, l.Quantity, l.UnitPrice)).ToList();
        var itemsById = await OrderDtoMapping.ResolveLineItemsAsync(_inventoryRepo, command.FarmId, lineItems, ct);

        var order = Domain.Entities.Order.Create(command.FarmId, req.SupplierId, lineItems);
        await _orderRepo.AddAsync(order, ct);

        return OrderDtoMapping.ToDto(order, supplier, itemsById);
    }
}
