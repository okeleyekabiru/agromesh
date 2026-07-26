namespace AgroMesh.Domain.ValueObjects;

/// <summary>
/// One line of an Order — a quantity of a specific InventoryItem at an agreed UnitPrice.
/// Stored as a jsonb column on Order (see OrderConfiguration), the same mapping shape as
/// Section.Crops/SectionCrop — no query ever needs to filter/join on an individual line item.
/// </summary>
public record OrderLineItem(string InventoryItemId, decimal Quantity, decimal UnitPrice)
{
    public decimal LineTotal => Quantity * UnitPrice;
}
