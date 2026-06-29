using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record InventoryItemDto(
    string Id,
    string FarmId,
    string Name,
    string Unit,
    InventoryCategory Category,
    decimal CurrentStock,
    decimal ReorderThreshold,
    bool IsLowStock,
    string? ChemicalProductId,
    string? ChemicalProductName,
    DateTime? LastMovementDate);

public record CreateInventoryItemRequest(string Name, string Unit, InventoryCategory Category, decimal CurrentStock, decimal ReorderThreshold, string? ChemicalProductId);

public record AdjustStockRequest(decimal NewQuantity, string? Notes);

public record StockMovementDto(string Id, string InventoryItemId, StockMovementType Type, decimal Quantity, string? ActivityLogId, string? TransactionId, string? Notes, DateTime Date);

public record LowStockAlertDto(string InventoryItemId, string InventoryItemName, decimal CurrentStock, string Unit, decimal ReorderThreshold);

public record ChemicalProductDto(string Id, string Name, string? Manufacturer, ChemicalCategory ChemType, int TypicalIntervalDays, bool Active);
