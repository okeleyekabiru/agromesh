namespace AgroMesh.Domain.ValueObjects;

public record InputUsed(string Item, double Quantity, string Unit, string? InventoryItemId = null);
