namespace AgroMesh.Application.DTOs;

/// <summary>S/N is the 1-based position of the line; Amount is Rate x Quantity to two decimals.</summary>
public record InvoiceLineItemDto(int SerialNumber, string Description, decimal Rate, decimal Quantity, decimal Amount);

public record InvoiceDto(
    string Id,
    string FarmId,
    int Number,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    string BillToName,
    string? BillToPhone,
    string? BillToEmail,
    string? BillToAddress,
    List<InvoiceLineItemDto> LineItems,
    decimal Subtotal,
    decimal VatPercent,
    decimal VatAmount,
    decimal Total,
    string? PaymentAccountName,
    string? PaymentAccountNumber,
    string? PaymentBankName,
    string? TermsAndNotes,
    string? SignatoryName,
    string TemplateKey,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>A row of the invoice list: enough to show a table without sending every line item.</summary>
public record InvoiceSummaryDto(
    string Id,
    int Number,
    string Reference,
    DateOnly Date,
    DateOnly DueDate,
    string BillToName,
    decimal Total,
    string TemplateKey,
    DateTime CreatedAt);

public record InvoiceLineItemRequest(string Description, decimal Rate, decimal Quantity);

/// <summary>
/// Used for both create (POST) and update (PUT, a full replace). Left out on create: date = today, dueDate = date + 14 days,
/// vatPercent = 0, templateKey = "ngf", and an all-blank payment block is filled from the farm business profile.
/// Left out on update: date and dueDate keep their values; everything else optional is cleared.
/// </summary>
public record SaveInvoiceRequest(
    DateOnly? Date,
    DateOnly? DueDate,
    string BillToName,
    string? BillToPhone,
    string? BillToEmail,
    string? BillToAddress,
    List<InvoiceLineItemRequest> LineItems,
    decimal? VatPercent,
    string? PaymentAccountName,
    string? PaymentAccountNumber,
    string? PaymentBankName,
    string? TermsAndNotes,
    string? SignatoryName,
    string? TemplateKey);

public record InvoiceTemplateDto(string Key, string Name, string Description, bool IsDefault);

/// <summary>A rendered invoice ready to send as a download.</summary>
public record InvoicePdfFile(byte[] Content, string FileName);
