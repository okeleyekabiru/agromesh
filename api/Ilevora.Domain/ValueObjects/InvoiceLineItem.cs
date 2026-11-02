namespace Ilevora.Domain.ValueObjects;

/// <summary>
/// One line of a sales Invoice. Stored as a jsonb list on Invoice like Order.LineItems. The serial number printed
/// beside a line is its position in the list, and its amount is worked out, so neither can disagree with the rate and quantity.
/// </summary>
public record InvoiceLineItem(string Description, decimal Rate, decimal Quantity)
{
    /// <summary>Rate x quantity to two decimals, rounded half away from zero, so what is printed is what is added up.</summary>
    public decimal Amount => Math.Round(Rate * Quantity, 2, MidpointRounding.AwayFromZero);
}
