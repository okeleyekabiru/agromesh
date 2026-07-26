using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Application.Mapping;

/// <summary>
/// Shared Order -> DTO mapping (and the InventoryItem/Supplier resolution it depends on), reused
/// by every Order command/query handler (Create/Place/Receive/Get*/GenerateInvoice) — avoids
/// duplicating "resolve each line item's InventoryItem, build OrderLineItemDto/OrderDto" across
/// five+ handlers. Mirrors AdjustInventoryStockCommandHandler.ToDto being reused by
/// ApproveStockAdjustmentCommandHandler. Lives outside the Commands/Queries namespaces (rather than
/// under Commands.Order, which both Commands AND Queries handlers need to reference) — a neutral
/// home avoids Queries.Order depending on something namespaced under Commands.Order.
/// </summary>
public static class OrderDtoMapping
{
    /// <summary>
    /// Resolves every distinct InventoryItemId referenced by <paramref name="lineItems"/>, farm-scoped.
    /// Throws before returning if ANY line item can't be resolved — callers that mutate stock per
    /// line item (ReceiveOrderCommand) must call this up front so a bad line item fails atomically,
    /// never after some lines have already been applied.
    /// </summary>
    public static async Task<Dictionary<string, InventoryItem>> ResolveLineItemsAsync(
        IInventoryRepository inventoryRepo, string farmId, List<OrderLineItem> lineItems, CancellationToken ct)
    {
        var itemsById = new Dictionary<string, InventoryItem>();
        foreach (var line in lineItems)
        {
            if (itemsById.ContainsKey(line.InventoryItemId)) continue;

            var item = await inventoryRepo.GetByIdAsync(line.InventoryItemId, ct)
                ?? throw new DomainException($"Inventory item '{line.InventoryItemId}' not found.");
            if (item.FarmId != farmId)
                throw new DomainException($"Inventory item '{line.InventoryItemId}' not found.");

            itemsById[line.InventoryItemId] = item;
        }
        return itemsById;
    }

    public static async Task<Supplier> ResolveSupplierAsync(
        ISupplierRepository supplierRepo, string farmId, string supplierId, CancellationToken ct)
    {
        var supplier = await supplierRepo.GetByIdAsync(supplierId, ct)
            ?? throw new DomainException($"Supplier '{supplierId}' not found.");
        if (supplier.FarmId != farmId)
            throw new DomainException($"Supplier '{supplierId}' not found.");
        return supplier;
    }

    public static Order EnsureFarmScoped(Order order, string farmId)
    {
        if (order.FarmId != farmId)
            throw new DomainException($"Order '{order.Id}' not found.");
        return order;
    }

    public static OrderDto ToDto(Order order, Supplier supplier, Dictionary<string, InventoryItem> itemsById) =>
        new(
            order.Id,
            order.FarmId,
            order.SupplierId,
            supplier.Name,
            order.Status.ToString(),
            order.LineItems.Select(l =>
            {
                var item = itemsById[l.InventoryItemId];
                return new OrderLineItemDto(l.InventoryItemId, item.Name, item.Unit, l.Quantity, l.UnitPrice, l.LineTotal);
            }).ToList(),
            order.Total,
            order.CreatedAt,
            order.UpdatedAt);
}
