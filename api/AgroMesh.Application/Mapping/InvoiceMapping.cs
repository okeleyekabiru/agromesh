using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Application.Mapping;

public static class InvoiceMapping
{
    public const int DefaultDueDays = 14;

    /// <summary>The default due date, refusing a date so late that adding the days would overflow.</summary>
    public static DateOnly DefaultDueDate(DateOnly date)
    {
        if (date.Year is < Invoice.MinYear or > Invoice.MaxYear)
            throw new DomainException($"Dates must be between {Invoice.MinYear} and {Invoice.MaxYear}.");
        return date.AddDays(DefaultDueDays);
    }

    public static string ResolveTemplateKey(IInvoiceTemplateRegistry registry, string? requested)
    {
        var key = string.IsNullOrWhiteSpace(requested) ? InvoiceTemplateKeys.Default : requested.Trim().ToLowerInvariant();
        if (!registry.Exists(key))
            throw new DomainException($"Unknown invoice template '{requested}'. Available: {string.Join(", ", registry.Templates.Select(t => t.Key))}.");
        return key;
    }

    public static InvoiceDetails ToDetails(SaveInvoiceRequest r, DateOnly date, DateOnly dueDate, string templateKey,
        string? accountName, string? accountNumber, string? bankName) =>
        new(date, dueDate, r.BillToName, r.BillToPhone, r.BillToEmail, r.BillToAddress,
            (r.LineItems ?? []).Select(l => new InvoiceLineItem(l.Description, l.Rate, l.Quantity)).ToList(),
            r.VatPercent ?? 0m,
            accountName, accountNumber, bankName,
            r.TermsAndNotes, r.SignatoryName, templateKey);

    public static Invoice EnsureFarmScoped(Invoice? invoice, string farmId, string invoiceId)
    {
        if (invoice is null || invoice.FarmId != farmId) throw new NotFoundException("Invoice", invoiceId);
        return invoice;
    }

    public static InvoiceDto ToDto(Invoice i) => new(
        i.Id, i.FarmId, i.Number, i.Reference, i.Date, i.DueDate,
        i.BillToName, i.BillToPhone, i.BillToEmail, i.BillToAddress,
        i.LineItems.Select((l, index) => new InvoiceLineItemDto(index + 1, l.Description, l.Rate, l.Quantity, l.Amount)).ToList(),
        i.Subtotal, i.VatPercent, i.VatAmount, i.Total,
        i.PaymentAccountName, i.PaymentAccountNumber, i.PaymentBankName,
        i.TermsAndNotes, i.SignatoryName, i.TemplateKey, i.CreatedAt, i.UpdatedAt);

    public static InvoiceSummaryDto ToSummary(Invoice i) =>
        new(i.Id, i.Number, i.Reference, i.Date, i.DueDate, i.BillToName, i.Total, i.TemplateKey, i.CreatedAt);
}
