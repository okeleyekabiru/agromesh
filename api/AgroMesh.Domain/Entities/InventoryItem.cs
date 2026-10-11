using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public string CategoryId { get; private set; } = string.Empty;
    public InventoryCategory Category { get; private set; } = null!;
    public decimal CurrentStock { get; private set; }
    public decimal ReorderThreshold { get; private set; }
    public string? ChemicalProductId { get; private set; }

    // Stock is counted in Unit (kg, L ...) but bought by the pack (a 50 kg bag, a 20 L container).
    // UnitCost is the running average price of one Unit, so using 38 kg of a bag costs 38 x UnitCost.
    public decimal PackSize { get; private set; } = 1;
    public string PackLabel { get; private set; } = "unit";
    public decimal UnitCost { get; private set; }

    public bool IsLowStock => CurrentStock <= ReorderThreshold;

    public List<StockMovement> Movements { get; private set; } = [];

    private InventoryItem() { }

    public static InventoryItem Create(
        string farmId,
        string name,
        string unit,
        string categoryId,
        decimal currentStock,
        decimal reorderThreshold,
        string? chemicalProductId = null)
    {
        return new InventoryItem
        {
            FarmId = farmId,
            Name = name,
            Unit = unit,
            CategoryId = categoryId,
            CurrentStock = currentStock,
            ReorderThreshold = reorderThreshold,
            ChemicalProductId = chemicalProductId
        };
    }

    public void AddStock(decimal quantity, string? notes = null)
    {
        if (quantity <= 0)
            throw new DomainException("Stock quantity to add must be positive.");
        CurrentStock += quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Describes how this item is bought, e.g. ("bag", 50) for a 50 kg bag of fertilizer.</summary>
    public void SetPack(string label, decimal size)
    {
        if (size <= 0)
            throw new DomainException("Pack size must be greater than zero.");
        PackLabel = string.IsNullOrWhiteSpace(label) ? "unit" : label.Trim();
        PackSize = size;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds bought stock at <paramref name="unitPrice"/> per Unit and folds it into the average cost.
    /// Stock that had no price yet takes the new price rather than diluting it.
    /// </summary>
    public void ReceiveStock(decimal quantity, decimal? unitPrice)
    {
        var before = CurrentStock;
        AddStock(quantity);
        if (unitPrice is null) return;
        if (unitPrice < 0)
            throw new DomainException("Price cannot be negative.");
        UnitCost = before <= 0 || UnitCost == 0
            ? unitPrice.Value
            : Math.Round((before * UnitCost + quantity * unitPrice.Value) / (before + quantity), 4);
    }

    /// <summary>Sets the price of one Unit directly (used when pricing stock that is already on hand).</summary>
    public void SetUnitCost(decimal unitCost)
    {
        if (unitCost < 0)
            throw new DomainException("Price cannot be negative.");
        UnitCost = unitCost;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DeductStock(decimal quantity)
    {
        if (quantity <= 0)
            throw new DomainException("Stock quantity to deduct must be positive.");
        if (CurrentStock < quantity)
            throw new DomainException($"Insufficient stock for '{Name}'. Available: {CurrentStock} {Unit}, requested: {quantity} {Unit}.");
        CurrentStock -= quantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AdjustStock(decimal newQuantity, string? notes = null)
    {
        if (newQuantity < 0)
            throw new DomainException("Stock cannot be adjusted to a negative value.");
        CurrentStock = newQuantity;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string name, decimal reorderThreshold)
    {
        Name = name;
        ReorderThreshold = reorderThreshold;
        UpdatedAt = DateTime.UtcNow;
    }
}
