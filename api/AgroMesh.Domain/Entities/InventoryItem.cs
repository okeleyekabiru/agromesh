using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

public class InventoryItem : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Unit { get; private set; } = string.Empty;
    public InventoryCategory Category { get; private set; }
    public decimal CurrentStock { get; private set; }
    public decimal ReorderThreshold { get; private set; }
    public string? ChemicalProductId { get; private set; }

    public bool IsLowStock => CurrentStock <= ReorderThreshold;

    public List<StockMovement> Movements { get; private set; } = [];

    private InventoryItem() { }

    public static InventoryItem Create(
        string farmId,
        string name,
        string unit,
        InventoryCategory category,
        decimal currentStock,
        decimal reorderThreshold,
        string? chemicalProductId = null)
    {
        return new InventoryItem
        {
            FarmId = farmId,
            Name = name,
            Unit = unit,
            Category = category,
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
