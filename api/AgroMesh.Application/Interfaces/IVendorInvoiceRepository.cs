using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface IVendorInvoiceRepository
{
    Task<VendorInvoice?> GetByIdAsync(string id, CancellationToken ct = default);
    /// <summary>Newest first. Optionally narrowed to one order and/or one status.</summary>
    Task<List<VendorInvoice>> GetByFarmIdAsync(string farmId, string? orderId = null, VendorInvoiceStatus? status = null, CancellationToken ct = default);
    /// <summary>True when this vendor already has a bill with this number on the farm (compared without regard to case).</summary>
    Task<bool> ExistsAsync(string farmId, string supplierId, string invoiceNumber, CancellationToken ct = default);
    Task<VendorInvoice> AddAsync(VendorInvoice invoice, CancellationToken ct = default);
    Task UpdateAsync(VendorInvoice invoice, CancellationToken ct = default);
    Task DeleteAsync(VendorInvoice invoice, CancellationToken ct = default);
}
