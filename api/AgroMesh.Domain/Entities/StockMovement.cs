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
    public string? Notes { get; private set; }
    public DateTime Date { get; private set; }

    private StockMovement() { }

    public static StockMovement CreateOut(string farmId, string inventoryItemId, decimal quantity, string? activityLogId = null, string? notes = null)
    {
        return new StockMovement
        {
            FarmId = farmId,
            InventoryItemId = inventoryItemId,
            Type = StockMovementType.Out,
            Quantity = quantity,
            ActivityLogId = activityLogId,
            Notes = notes,
            Date = DateTime.UtcNow
        };
    }

    public static StockMovement CreateIn(string farmId, string inventoryItemId, decimal quantity, string? transactionId = null, string? notes = null)
    {
        return new StockMovement
        {
            FarmId = farmId,
            InventoryItemId = inventoryItemId,
            Type = StockMovementType.In,
            Quantity = quantity,
            TransactionId = transactionId,
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
