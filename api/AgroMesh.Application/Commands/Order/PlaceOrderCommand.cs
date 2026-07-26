using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Order;

public record PlaceOrderCommand(string FarmId, string OrderId) : IRequest<OrderDto>;

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public PlaceOrderCommandHandler(IOrderRepository orderRepo, ISupplierRepository supplierRepo, IInventoryRepository inventoryRepo)
    {
        _orderRepo = orderRepo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<OrderDto> Handle(PlaceOrderCommand command, CancellationToken ct)
    {
        var order = await _orderRepo.GetByIdAsync(command.OrderId, ct)
            ?? throw new DomainException($"Order '{command.OrderId}' not found.");
        OrderDtoMapping.EnsureFarmScoped(order, command.FarmId);

        // order.Place() itself re-validates the Draft->Placed transition (defence in depth) — this
        // check ordering (validate/throw before any repository write) is what keeps
        // PlaceOrder_FromPlaced_ThrowsDomainException_NoUpdateCall's "no side effects" guarantee true.
        order.Place();
        await _orderRepo.UpdateAsync(order, ct);

        var supplier = await OrderDtoMapping.ResolveSupplierAsync(_supplierRepo, command.FarmId, order.SupplierId, ct);
        var itemsById = await OrderDtoMapping.ResolveLineItemsAsync(_inventoryRepo, command.FarmId, order.LineItems, ct);
        return OrderDtoMapping.ToDto(order, supplier, itemsById);
    }
}
