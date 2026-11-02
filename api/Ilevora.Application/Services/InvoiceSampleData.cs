using Ilevora.Domain.Entities;
using Ilevora.Domain.ValueObjects;

namespace Ilevora.Application.Services;

/// <summary>A made-up invoice used to show what a template looks like. Never saved. The payment block uses the farm's own bank details when it has them.</summary>
public static class InvoiceSampleData
{
    public static Invoice Build(Farm farm, string templateKey)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var hasBank = !string.IsNullOrWhiteSpace(farm.BankAccountNumber);

        var details = new InvoiceDetails(
            today, today.AddDays(14),
            "Sample Customer Ltd", "+234 800 000 0000", "accounts@customer.example", "15 Market Road, Lagos",
            [
                new InvoiceLineItem("Fresh pineapples, grade A (crate)", 4500m, 20m),
                new InvoiceLineItem("Cassava tubers (bag of 50 kg)", 12500m, 8m),
                new InvoiceLineItem("Plantain bunches", 2500m, 40m),
                new InvoiceLineItem("Farm-gate delivery", 15000m, 1m),
                new InvoiceLineItem("Packaging and handling", 6000m, 1m),
                new InvoiceLineItem("Organic fertiliser (25 kg)", 9800m, 3m),
            ],
            7.5m,
            hasBank ? farm.BankAccountName : "Sample Farm Account",
            hasBank ? farm.BankAccountNumber : "0123456789",
            hasBank ? farm.BankName : "Sample Bank",
            "Payment is due within 14 days of the invoice date. Goods remain the property of the seller until paid in full.",
            "Authorised Signatory",
            templateKey);

        return Invoice.Create(farm.Id, 1, details, null);
    }
}
