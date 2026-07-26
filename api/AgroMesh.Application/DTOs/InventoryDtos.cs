using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record InventoryItemDto(
    string Id,
    string FarmId,
    string Name,
    string Unit,
    string CategoryId,
    string CategoryName,
    decimal CurrentStock,
    decimal ReorderThreshold,
    bool IsLowStock,
    string? ChemicalProductId,
    string? ChemicalProductName,
    DateTime? LastMovementDate);

/// <summary>
/// The unified "stock in" request — used both to onboard a brand-new item and to restock an
/// existing one. <see cref="Quantity"/> is the amount being added: for a new item it becomes the
/// opening stock, for an existing one it's added to the current stock via InventoryItem.AddStock.
/// <see cref="ExistingItemId"/> is the explicit item-picker path; when omitted, the handler falls
/// back to a case-insensitive name match on the farm before creating a new item.
/// </summary>
public record CreateInventoryItemRequest(
    string Name,
    string Unit,
    string CategoryId,
    decimal Quantity,
    decimal ReorderThreshold,
    string? ChemicalProductId,
    string? ExistingItemId = null);

public record InventoryCategoryDto(string Id, string FarmId, string Name, bool IsDefault);

public record CreateInventoryCategoryRequest(string Name);

public record AdjustStockRequest(decimal NewQuantity, string? Notes);

/// <summary>
/// Manual "pick / issue from stock" request — a genuinely separate code path from the
/// activity-log-driven auto-deduction (IInventoryDeductionService). Always records an Out
/// StockMovement via InventoryItem.DeductStock; insufficient stock throws DomainException (422)
/// rather than being caught into a soft warning like the auto-deduction path does.
///
/// <see cref="SectionId"/> (required) / <see cref="BlockId"/> (required only when the section has
/// blocks) record which farm area the picked stock is going to — who performed the pick is no
/// longer client-supplied (see the removed WorkerId field): PickStockCommandHandler now
/// auto-attributes the authenticated caller via ICurrentUserAccessor instead of trusting a
/// client-chosen Worker.
/// </summary>
public record PickStockRequest(decimal Quantity, string SectionId, string? BlockId, string Purpose);

/// <summary>
/// <see cref="LowStockWarning"/> is non-null only when the pick pushed the item's stock to/below its
/// reorder threshold — a warning, not a block (the pick already succeeded by the time this is set).
/// </summary>
public record PickStockResponse(InventoryItemDto Item, string? LowStockWarning);

public record StockMovementDto(string Id, string InventoryItemId, StockMovementType Type, decimal Quantity, string? ActivityLogId, string? TransactionId, string? OrderId, string? Notes, DateTime Date);

public record LowStockAlertDto(string InventoryItemId, string InventoryItemName, decimal CurrentStock, string Unit, decimal ReorderThreshold);

public record ChemicalProductDto(string Id, string Name, string? Manufacturer, ChemicalCategory ChemType, int TypicalIntervalDays, bool Active);

/// <summary>
/// <see cref="Status"/>/<see cref="RequestedByUserId"/>/<see cref="ReviewedByUserId"/> follow the
/// same "build the string explicitly rather than serialize the raw enum" convention as
/// <c>AuthUserDto.Role</c> — see that DTO's doc comment. <see cref="CurrentStock"/> is the item's
/// stock *at response time* (not locked at request time) — context for the reviewer, not a
/// guarantee it hasn't moved since submission.
/// </summary>
public record StockAdjustmentRequestDto(
    string Id,
    string FarmId,
    string InventoryItemId,
    string InventoryItemName,
    string Unit,
    decimal CurrentStock,
    decimal RequestedQuantity,
    string Status,
    string Notes,
    string RequestedByUserId,
    string? ReviewedByUserId,
    DateTime? ReviewedAt,
    string? ReviewNotes,
    DateTime CreatedAt);

/// <summary>Body for both approve and reject — the admin's optional comment, most useful on reject.</summary>
public record ReviewStockAdjustmentRequest(string? ReviewNotes);
