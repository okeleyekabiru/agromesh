namespace AgroMesh.Application.DTOs;

public record OrderLineItemDto(
    string InventoryItemId,
    string InventoryItemName,
    string Unit,
    decimal Quantity,
    decimal UnitPrice,
    decimal LineTotal);

/// <summary>
/// <see cref="Status"/> is a plain string ("Draft"/"Placed"/"Received") built explicitly via
/// <c>order.Status.ToString()</c> rather than serializing the raw enum — same convention as
/// StockAdjustmentRequestDto.Status/AuthUserDto.Role (no JsonStringEnumConverter configured).
/// </summary>
public record OrderDto(
    string Id,
    string FarmId,
    string SupplierId,
    string SupplierName,
    string Status,
    List<OrderLineItemDto> LineItems,
    decimal Total,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Number = 0,
    string Reference = "",
    bool IsPaid = false,
    DateTime? PaidAt = null,
    string? PaymentTransactionId = null);

public record CreateOrderLineItemRequest(string InventoryItemId, decimal Quantity, decimal UnitPrice);

public record CreateOrderRequest(string SupplierId, List<CreateOrderLineItemRequest> LineItems);
