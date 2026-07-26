using AgroMesh.Domain.Enums;

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
    // client-supplied WorkerId. Which Section/Block the picked stock is going to.
    public string? PerformedByUserId { get; private set; }
    public string? SectionId { get; private set; }
    public string? BlockId { get; private set; }
    public string? Notes { get; private set; }
    public DateTime Date { get; private set; }

    private StockMovement() { }

    /// <summary>
    /// <paramref name="workerId"/> is unused by any current call site (kept as a harmless, nullable
    /// column rather than a disruptive drop-column migration — see PickStockCommand's doc comment).
    /// <paramref name="performedByUserId"/>/<paramref name="sectionId"/>/<paramref name="blockId"/>
    /// are populated by the manual pick/issue path (PickStockCommand); all left null for the
    /// activity-log-driven auto-deduction path (InventoryDeductionService), which has no user/section
    /// context of its own to attribute a movement to.
    /// </summary>
    public static StockMovement CreateOut(
        string farmId, string inventoryItemId, decimal quantity, string? activityLogId = null, string? notes = null,
        string? workerId = null, string? performedByUserId = null, string? sectionId = null, string? blockId = null)
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
            SectionId = sectionId,
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
