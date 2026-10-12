using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Order;

/// <summary>
/// Converts a Placed order's line items into real stock — exactly the RecordInventoryReceiptCommand
/// pattern (item.AddStock -> _inventoryRepo.UpdateAsync -> StockMovement.CreateIn ->
/// _inventoryRepo.AddMovementAsync), once per line item, no arithmetic duplicated. Never calls
/// another MediatR command internally (this codebase's every _mediator.Send(...) call originates
/// from a Controller — this handler doesn't break that convention).
/// </summary>
public record ReceiveOrderCommand(string FarmId, string OrderId) : IRequest<OrderDto>;

public class ReceiveOrderCommandHandler : IRequestHandler<ReceiveOrderCommand, OrderDto>
{
    private readonly IOrderRepository _orderRepo;
    private readonly ISupplierRepository _supplierRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public ReceiveOrderCommandHandler(IOrderRepository orderRepo, ISupplierRepository supplierRepo, IInventoryRepository inventoryRepo)
    {
        _orderRepo = orderRepo;
        _supplierRepo = supplierRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<OrderDto> Handle(ReceiveOrderCommand command, CancellationToken ct)
    {
        var order = await _orderRepo.GetByIdAsync(command.OrderId, ct)
            ?? throw new DomainException($"Order '{command.OrderId}' not found.");
        OrderDtoMapping.EnsureFarmScoped(order, command.FarmId);

        // Checked BEFORE any line item is resolved or any stock mutated — receiving a Draft order,
        // or re-receiving an already-Received one, must never partially apply stock. order.Receive()
        // re-validates this too (defence in depth for the entity's own invariant), but by then it
        // would already be too late to stop the stock side effects below if this check weren't first
        // — same ordering ApproveStockAdjustmentCommandHandler uses for its Pending guard.
        if (order.Status != OrderStatus.Placed)
            throw new DomainException($"Only a Placed order can be received. This order is {order.Status}.");

        // Resolve every line item's InventoryItem up front — if any is unresolvable (deleted,
        // wrong farm), this throws before a single AddStock call, so the receipt fails atomically
        // rather than leaving some items already restocked.
        var itemsById = await OrderDtoMapping.ResolveLineItemsAsync(_inventoryRepo, command.FarmId, order.LineItems, ct);

        foreach (var line in order.LineItems)
        {
            var item = itemsById[line.InventoryItemId];
            // The order's unit price becomes the stock's cost (a weighted average with what is already on hand), so each
            // activity that later uses this stock carries the cost of what was actually bought.
            item.ReceiveStock(line.Quantity, line.UnitPrice);
            await _inventoryRepo.UpdateAsync(item, ct);

            var movement = StockMovement.CreateIn(item.FarmId, item.Id, line.Quantity, notes: $"Received from order {order.Id}", orderId: order.Id);
            await _inventoryRepo.AddMovementAsync(movement, ct);
        }

        order.Receive();
        await _orderRepo.UpdateAsync(order, ct);

        var supplier = await OrderDtoMapping.ResolveSupplierAsync(_supplierRepo, command.FarmId, order.SupplierId, ct);
        return OrderDtoMapping.ToDto(order, supplier, itemsById);
    }
}
