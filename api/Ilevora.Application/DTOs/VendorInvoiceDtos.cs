namespace Ilevora.Application.DTOs;

/// <summary>
/// A vendor's invoice for an order. <see cref="Status"/> is "Unpaid" or "Paid" (a plain string, like OrderDto.Status).
/// OrderTotal and OrderIsPaid are the order's own figures, so a client can flag a bill whose amount differs from the order.
/// </summary>
public record VendorInvoiceDto(
    string Id,
    string FarmId,
    string OrderId,
    string OrderReference,
    decimal OrderTotal,
    bool OrderIsPaid,
    string SupplierId,
    string VendorName,
    string InvoiceNumber,
    decimal Amount,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string Status,
    DateTime? PaidAt,
    bool IsOverdue,
    string? Notes,
    string FileName,
    string ContentType,
    long FileSize,
    string? UploadedByUserId,
    DateTime CreatedAt);

/// <summary>The form fields sent with the file. VendorName defaults to the order's supplier.</summary>
public record UploadVendorInvoiceFields(
    string? VendorName,
    string InvoiceNumber,
    decimal Amount,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string? Notes);

/// <summary>Status is "Paid" or "Unpaid" (case-insensitive). PaidAt is optional and only used when marking paid; it defaults to now.</summary>
public record SetVendorInvoiceStatusRequest(string Status, DateTime? PaidAt);

public record VendorInvoiceFile(byte[] Content, string ContentType, string FileName);
