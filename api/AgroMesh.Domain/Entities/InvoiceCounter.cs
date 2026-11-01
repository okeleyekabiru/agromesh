namespace AgroMesh.Domain.Entities;

/// <summary>
/// The last invoice number handed out for a farm. Kept apart from Farm on purpose: it is advanced with a single atomic
/// statement by the invoice repository, and a whole-row Farm update elsewhere must never be able to write a stale value
/// back and make numbers repeat. Not touched by the domain code; see IInvoiceRepository.ReserveNextNumberAsync.
/// </summary>
public class InvoiceCounter
{
    public string FarmId { get; private set; } = string.Empty;
    public int LastNumber { get; private set; }

    private InvoiceCounter() { }
}
