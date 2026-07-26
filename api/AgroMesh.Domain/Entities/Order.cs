using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Entities;

/// <summary>
/// A farm-scoped purchase order placed with a Supplier. Status only ever moves forward
/// (Draft -> Placed -> Received) via Place()/Receive(), which enforce their own transition
/// invariants the same way StockAdjustmentRequest.Approve/Reject do — throwing DomainException
/// on an invalid transition rather than leaving that check to callers. Receiving an order is what
/// converts its LineItems into real stock (see ReceiveOrderCommand) — this entity itself has no
/// knowledge of InventoryItem/StockMovement, it only guards its own status.
/// </summary>
public class Order : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string SupplierId { get; private set; } = string.Empty;
    public OrderStatus Status { get; private set; }
    public List<OrderLineItem> LineItems { get; private set; } = [];

    // Computed, not stored — same convention as InventoryItem.IsLowStock.
    public decimal Total => LineItems.Sum(l => l.LineTotal);

    private Order() { }

    public static Order Create(string farmId, string supplierId, List<OrderLineItem> lineItems)
    {
        ValidateLineItems(lineItems);

        return new Order
        {
            FarmId = farmId,
            SupplierId = supplierId,
            LineItems = lineItems,
            Status = OrderStatus.Draft
        };
    }

    public void Place()
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException($"Only a Draft order can be placed. This order is {Status}.");
        Status = OrderStatus.Placed;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Receive()
    {
        if (Status != OrderStatus.Placed)
            throw new DomainException($"Only a Placed order can be received. This order is {Status}.");
        Status = OrderStatus.Received;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateLineItems(List<OrderLineItem> lineItems)
    {
        if (lineItems == null || lineItems.Count == 0)
            throw new DomainException("An order must have at least one line item.");

        foreach (var line in lineItems)
        {
            if (line.Quantity <= 0)
                throw new DomainException("Each order line item must have a positive quantity.");
            if (line.UnitPrice < 0)
                throw new DomainException("Each order line item's unit price cannot be negative.");
        }
    }
}
