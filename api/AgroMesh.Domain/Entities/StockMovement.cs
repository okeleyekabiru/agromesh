using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

public class StockMovement : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string InventoryItemId { get; private set; } = string.Empty;
    public StockMovementType Type { get; private set; }
    public decimal Quantity { get; private set; }
    public string? ActivityLogId { get; private set; }
    public string? TransactionId { get; private set; }
    public string? WorkerId { get; private set; }
    public string? OrderId { get; private set; }
    // Who (authenticated company/office user, via ICurrentUserAccessor — not a Worker) actually
    // performed a manual pick — see PickStockCommand's doc comment for why this replaced a
    // client-supplied WorkerId. Which Block the picked stock is going to.
    public string? PerformedByUserId { get; private set; }
    public string? BlockId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime Date { get; private set; }

    // A manual pick (an Out movement with a picker) takes stock off the shelf and onto the farm.
    // Stock is already deducted at pick time, so what happens to it afterwards is tracked here:
    // used in an activity, or returned to the store. Anything left is still "out on the farm".
    public decimal UsedQuantity { get; private set; }
    public decimal ReturnedQuantity { get; private set; }

    public bool IsPick => Type == StockMovementType.Out && PerformedByUserId != null;
    public decimal Outstanding => IsPick ? Quantity - UsedQuantity - ReturnedQuantity : 0;

    /// <summary>Records that part of this pick was consumed by an activity (stock was already deducted).</summary>
    public void RecordUsage(decimal quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity used must be positive.");
        if (quantity > Outstanding) throw new DomainException("Cannot use more than is still out on the farm.");
        UsedQuantity += quantity;
    }

    /// <summary>Records that part of this pick was handed back to the store.</summary>
    public void RecordReturn(decimal quantity)
    {
        if (quantity <= 0) throw new DomainException("Quantity returned must be positive.");
        if (quantity > Outstanding) throw new DomainException("Cannot return more than is still out on the farm.");
        ReturnedQuantity += quantity;
    }

    private StockMovement() { }

    /// <summary>
    /// <paramref name="workerId"/> is unused by any current call site (kept as a harmless, nullable
    /// column rather than a disruptive drop-column migration — see PickStockCommand's doc comment).
    /// <paramref name="performedByUserId"/>/<paramref name="blockId"/>
    /// are populated by the manual pick/issue path (PickStockCommand); all left null for the
    /// activity-log-driven auto-deduction path (InventoryDeductionService), which has no user/block
    /// context of its own to attribute a movement to.
    /// </summary>
    public static StockMovement CreateOut(
        string farmId, string inventoryItemId, decimal quantity, string? activityLogId = null, string? notes = null,
        string? workerId = null, string? performedByUserId = null, string? blockId = null)
    {
        return new StockMovement
        {
            FarmId = farmId,
            InventoryItemId = inventoryItemId,
            Type = StockMovementType.Out,
            Quantity = quantity,
            ActivityLogId = activityLogId,
            WorkerId = workerId,
            PerformedByUserId = performedByUserId,
            BlockId = blockId,
            Notes = notes,
            Date = DateTime.UtcNow
        };
    }

    /// <summary>
    /// <paramref name="orderId"/> is set when this "In" movement was produced by
    /// ReceiveOrderCommand (converting a placed Order's line items into stock) — left null for the
    /// pre-existing RecordInventoryReceiptCommand path, which has no order context of its own.
    /// </summary>
    public static StockMovement CreateIn(string farmId, string inventoryItemId, decimal quantity, string? transactionId = null, string? notes = null, string? orderId = null)
    {
        return new StockMovement
        {
            FarmId = farmId,
            InventoryItemId = inventoryItemId,
            Type = StockMovementType.In,
            Quantity = quantity,
            TransactionId = transactionId,
            OrderId = orderId,
            Notes = notes,
            Date = DateTime.UtcNow
        };
    }

    /// <summary>Leftover stock handed back to the store after a pick. Adds to stock (see ReturnStockCommand).</summary>
    public static StockMovement CreateReturn(string farmId, string inventoryItemId, decimal quantity, string blockId, string performedByUserId, string? notes = null)
    {
        return new StockMovement
        {
            FarmId = farmId,
            InventoryItemId = inventoryItemId,
            Type = StockMovementType.Return,
            Quantity = quantity,
            BlockId = blockId,
            PerformedByUserId = performedByUserId,
            Notes = notes,
            Date = DateTime.UtcNow
        };
    }

    public static StockMovement CreateAdjustment(string farmId, string inventoryItemId, decimal quantity, string? notes = null)
    {
        return new StockMovement
        {
            FarmId = farmId,
            InventoryItemId = inventoryItemId,
            Type = StockMovementType.Adjustment,
            Quantity = quantity,
            Notes = notes,
            Date = DateTime.UtcNow
        };
    }
}
