using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

/// <summary>What a person types when attaching a vendor's invoice to an order.</summary>
public record VendorInvoiceDetails(
    string VendorName,
    string InvoiceNumber,
    decimal Amount,
    DateOnly InvoiceDate,
    DateOnly? DueDate,
    string? Notes);

/// <summary>
/// An invoice a vendor (Supplier) issued to the farm for a completed (Received) Order, kept with the file the vendor sent
/// (a StoredFile). The opposite direction to Invoice, which the farm issues. It is a record of a document: it does not post
/// to the farm's books. Money spent on an order is already recognised through bank transactions matched to the order and the
/// stock's cost when it is used, so counting this amount as well would count it twice. Status is a person's note of whether
/// the bill has been settled.
/// </summary>
public class VendorInvoice : BaseEntity
{
    /// <summary>Column width of the key: the longest number (100) plus room for the suffix a legacy duplicate is given by the migration.</summary>
    public const int MaxNumberKeyLength = 140;

    public string FarmId { get; private set; } = string.Empty;
    public string OrderId { get; private set; } = string.Empty;
    public string SupplierId { get; private set; } = string.Empty;

    public string FileId { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long FileSize { get; private set; }

    public string VendorName { get; private set; } = string.Empty;
    public string InvoiceNumber { get; private set; } = string.Empty;
    /// <summary>The number as compared for duplicates: trimmed and upper-cased. A unique index on (farm, vendor, this key) makes "one bill per vendor number" hold even when two uploads race.</summary>
    public string InvoiceNumberKey { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateOnly InvoiceDate { get; private set; }
    public DateOnly? DueDate { get; private set; }
    public VendorInvoiceStatus Status { get; private set; } = VendorInvoiceStatus.Unpaid;
    public DateTime? PaidAt { get; private set; }
    public string? Notes { get; private set; }
    public string? UploadedByUserId { get; private set; }

    private VendorInvoice() { }

    public static VendorInvoice Create(string farmId, string orderId, string supplierId, string fileId, string fileName,
        string contentType, long fileSize, VendorInvoiceDetails details, string? uploadedByUserId)
    {
        if (string.IsNullOrWhiteSpace(fileId))
            throw new DomainException("A vendor invoice needs its file.");

        var vendor = Clean(details.VendorName, 200, "Vendor name") ?? throw new DomainException("Vendor name is required.");
        var number = Clean(details.InvoiceNumber, 100, "Invoice number") ?? throw new DomainException("Invoice number is required.");
        var amount = Math.Round(details.Amount, 2, MidpointRounding.AwayFromZero);
        if (amount <= 0) throw new DomainException("The invoice amount must be more than zero.");
        if (amount >= 1_000_000_000_000m) throw new DomainException("The invoice amount is too large.");
        if (details.InvoiceDate.Year is < Invoice.MinYear or > Invoice.MaxYear || details.DueDate is { Year: < Invoice.MinYear or > Invoice.MaxYear })
            throw new DomainException($"Dates must be between {Invoice.MinYear} and {Invoice.MaxYear}.");
        if (details.DueDate is { } due && due < details.InvoiceDate)
            throw new DomainException("The due date cannot be before the invoice date.");
        var notes = Clean(details.Notes, 1000, "Notes");

        return new VendorInvoice
        {
            FarmId = farmId,
            OrderId = orderId,
            SupplierId = supplierId,
            FileId = fileId,
            FileName = fileName,
            ContentType = contentType,
            FileSize = fileSize,
            VendorName = vendor,
            InvoiceNumber = number,
            InvoiceNumberKey = NormaliseNumber(number),
            Amount = amount,
            InvoiceDate = details.InvoiceDate,
            DueDate = details.DueDate,
            Notes = notes,
            UploadedByUserId = uploadedByUserId,
        };
    }

    public static string NormaliseNumber(string number) => number.Trim().ToUpperInvariant();

    /// <summary>Overdue means still unpaid after its due date. A bill with no due date is never overdue.</summary>
    public bool IsOverdue(DateOnly today) => Status == VendorInvoiceStatus.Unpaid && DueDate is { } due && due < today;

    /// <summary>Marking an already paid invoice paid again changes nothing, so the first payment date is kept.</summary>
    public void MarkPaid(DateTime? paidAt)
    {
        if (Status == VendorInvoiceStatus.Paid) return;
        Status = VendorInvoiceStatus.Paid;
        PaidAt = paidAt is null ? DateTime.UtcNow : DateTime.SpecifyKind(paidAt.Value, DateTimeKind.Utc);
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkUnpaid()
    {
        if (Status == VendorInvoiceStatus.Unpaid) return;
        Status = VendorInvoiceStatus.Unpaid;
        PaidAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    private static string? Clean(string? value, int max, string label)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > max) throw new DomainException($"{label} must be at most {max} characters.");
        return trimmed;
    }
}
