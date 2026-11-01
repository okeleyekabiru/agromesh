using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Entities;

/// <summary>The invoice layouts that can be rendered. A new template gets a key here and a renderer in Infrastructure.</summary>
public static class InvoiceTemplateKeys
{
    /// <summary>"NGF - Invoice", the Nature's Gold Farm layout.</summary>
    public const string Ngf = "ngf";
    public const string Default = Ngf;

    public const string Band = "band";
    public const string Sidebar = "sidebar";
    public const string Callout = "callout";
    public const string Minimal = "minimal";
    public const string Cards = "cards";
    public const string Ledger = "ledger";
    public const string Angle = "angle";
    public const string Anchor = "anchor";
    public const string Duo = "duo";
}

/// <summary>Everything a user types into an invoice. Create and Update both take it so they cannot validate differently.</summary>
public record InvoiceDetails(
    DateOnly Date,
    DateOnly DueDate,
    string BillToName,
    string? BillToPhone,
    string? BillToEmail,
    string? BillToAddress,
    List<InvoiceLineItem> LineItems,
    decimal VatPercent,
    string? PaymentAccountName,
    string? PaymentAccountNumber,
    string? PaymentBankName,
    string? TermsAndNotes,
    string? SignatoryName,
    string TemplateKey,
    string? PrimaryColor = null,
    string? AccentColor = null);

/// <summary>
/// An invoice the farm issues to a customer (the opposite direction to a VendorInvoice, which a supplier issues to the farm).
/// Numbered per farm like Order. The payment block is a copy taken when the invoice is written, so changing the farm's
/// bank account later does not rewrite invoices already sent. Subtotal, VAT and total are computed, not stored.
/// </summary>
public class Invoice : BaseEntity
{
    public const int MaxLineItems = 200;
    public const int MinYear = 2000;
    public const int MaxYear = 2100;
    // Keep every figure printable in the PDF table and TOTAL band without wrapping or overflowing.
    public const decimal MaxRate = 100_000_000m;
    public const decimal MaxQuantity = 1_000_000m;
    public const decimal MaxLineAmount = 1_000_000_000m;
    public const decimal MaxSubtotal = 10_000_000_000m;

    public string FarmId { get; private set; } = string.Empty;
    public int Number { get; private set; }
    public string Reference => $"INV-{Number:D4}";

    public DateOnly Date { get; private set; }
    public DateOnly DueDate { get; private set; }

    public string BillToName { get; private set; } = string.Empty;
    public string? BillToPhone { get; private set; }
    public string? BillToEmail { get; private set; }
    public string? BillToAddress { get; private set; }

    public List<InvoiceLineItem> LineItems { get; private set; } = [];
    public decimal VatPercent { get; private set; }

    public string? PaymentAccountName { get; private set; }
    public string? PaymentAccountNumber { get; private set; }
    public string? PaymentBankName { get; private set; }

    public string? TermsAndNotes { get; private set; }
    public string? SignatoryName { get; private set; }
    public string TemplateKey { get; private set; } = InvoiceTemplateKeys.Default;

    /// <summary>Colours chosen for this invoice ("#RRGGBB"). Null means not overridden: the farm brand colour applies, else the template's own.</summary>
    public string? PrimaryColor { get; private set; }
    public string? AccentColor { get; private set; }
    public string? CreatedByUserId { get; private set; }

    public decimal Subtotal => LineItems.Sum(l => l.Amount);
    public decimal VatAmount => Math.Round(Subtotal * VatPercent / 100m, 2, MidpointRounding.AwayFromZero);
    public decimal Total => Subtotal + VatAmount;

    private Invoice() { }

    public static Invoice Create(string farmId, int number, InvoiceDetails details, string? createdByUserId)
    {
        if (number < 1) throw new DomainException("An invoice number must be 1 or more.");
        var invoice = new Invoice { FarmId = farmId, Number = number, CreatedByUserId = createdByUserId };
        invoice.Update(details);
        return invoice;
    }

    /// <summary>Replaces everything the user wrote. Nothing changes if any of it is invalid.</summary>
    public void Update(InvoiceDetails d)
    {
        var billTo = Clean(d.BillToName, 200, "Bill-to name") ?? throw new DomainException("Bill-to name is required.");
        var phone = Clean(d.BillToPhone, 60, "Bill-to phone");
        var email = Clean(d.BillToEmail, 200, "Bill-to email");
        var address = Clean(d.BillToAddress, 500, "Bill-to address");
        if (email is not null && !Utilities.EmailAddress.IsPlausible(email)) throw new DomainException("The bill-to email address is not valid.");
        var accountName = Clean(d.PaymentAccountName, 150, "Payment account name");
        var accountNumber = Clean(d.PaymentAccountNumber, 40, "Payment account number");
        var bankName = Clean(d.PaymentBankName, 100, "Payment bank name");
        var terms = Clean(d.TermsAndNotes, 2000, "Terms and notes");
        var signatory = Clean(d.SignatoryName, 150, "Signatory name");
        var primary = Utilities.BrandColor.Normalise(d.PrimaryColor, "Invoice primary colour");
        var accent = Utilities.BrandColor.Normalise(d.AccentColor, "Invoice accent colour");

        if (d.Date.Year is < MinYear or > MaxYear || d.DueDate.Year is < MinYear or > MaxYear)
            throw new DomainException($"Dates must be between {MinYear} and {MaxYear}.");
        if (d.DueDate < d.Date) throw new DomainException("The due date cannot be before the invoice date.");
        if (d.VatPercent < 0 || d.VatPercent > 100) throw new DomainException("VAT must be between 0 and 100 percent.");
        var items = ValidateLineItems(d.LineItems);
        if (string.IsNullOrWhiteSpace(d.TemplateKey)) throw new DomainException("An invoice template is required.");

        Date = d.Date;
        DueDate = d.DueDate;
        BillToName = billTo;
        BillToPhone = phone;
        BillToEmail = email;
        BillToAddress = address;
        LineItems = items;
        VatPercent = d.VatPercent;
        PaymentAccountName = accountName;
        PaymentAccountNumber = accountNumber;
        PaymentBankName = bankName;
        TermsAndNotes = terms;
        SignatoryName = signatory;
        TemplateKey = d.TemplateKey.Trim().ToLowerInvariant();
        PrimaryColor = primary;
        AccentColor = accent;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Puts the number reserved from the farm counter on a freshly validated invoice.</summary>
    public void AssignNumber(int number)
    {
        if (number < 1) throw new DomainException("An invoice number must be 1 or more.");
        Number = number;
    }

    private static List<InvoiceLineItem> ValidateLineItems(List<InvoiceLineItem>? lineItems)
    {
        if (lineItems is null || lineItems.Count == 0)
            throw new DomainException("An invoice must have at least one line item.");
        if (lineItems.Count > MaxLineItems)
            throw new DomainException($"An invoice can have at most {MaxLineItems} line items.");

        var result = new List<InvoiceLineItem>(lineItems.Count);
        foreach (var line in lineItems)
        {
            var description = Clean(line.Description, 500, "Line item description")
                ?? throw new DomainException("Each line item needs a description.");
            if (line.Rate < 0) throw new DomainException("A line item rate cannot be negative.");
            if (line.Quantity <= 0) throw new DomainException("A line item quantity must be more than zero.");
            if (line.Rate >= MaxRate) throw new DomainException($"A line item rate must be below {MaxRate:N0}.");
            if (line.Quantity >= MaxQuantity) throw new DomainException($"A line item quantity must be below {MaxQuantity:N0}.");
            if (line.Amount >= MaxLineAmount) throw new DomainException($"A line item amount must be below {MaxLineAmount:N0}.");
            result.Add(line with { Description = description });
        }
        if (result.Sum(l => l.Amount) >= MaxSubtotal)
            throw new DomainException($"The invoice subtotal must be below {MaxSubtotal:N0}.");
        return result;
    }

    private static string? Clean(string? value, int max, string label)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed)) return null;
        if (trimmed.Length > max) throw new DomainException($"{label} must be at most {max} characters.");
        return trimmed;
    }
}
