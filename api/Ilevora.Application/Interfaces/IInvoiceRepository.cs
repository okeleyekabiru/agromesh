using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(string id, CancellationToken ct = default);
    /// <summary>Newest first (highest number first).</summary>
    Task<List<Invoice>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    /// <summary>
    /// Hands out the next invoice number for this farm (1, 2, 3...) from a per-farm counter, atomically. A number is spent the moment
    /// it is handed out and never given again, even if that invoice is later deleted or never saved: an invoice number that has
    /// been sent to a customer must not be reused. Gaps are possible, repeats are not.
    /// </summary>
    Task<int> ReserveNextNumberAsync(string farmId, CancellationToken ct = default);
    /// <summary>Saves the invoice under the number reserved for it.</summary>
    Task<Invoice> AddAsync(Invoice invoice, CancellationToken ct = default);
    Task UpdateAsync(Invoice invoice, CancellationToken ct = default);
    Task DeleteAsync(Invoice invoice, CancellationToken ct = default);
}
