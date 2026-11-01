using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class VendorInvoiceRepository : IVendorInvoiceRepository
{
    private readonly AgroMeshDbContext _db;

    public VendorInvoiceRepository(AgroMeshDbContext db) => _db = db;

    public async Task<VendorInvoice?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.VendorInvoices.FindAsync([id], ct);

    public async Task<List<VendorInvoice>> GetByFarmIdAsync(string farmId, string? orderId = null, VendorInvoiceStatus? status = null, CancellationToken ct = default) =>
        await _db.VendorInvoices
            .Where(v => v.FarmId == farmId && (orderId == null || v.OrderId == orderId) && (status == null || v.Status == status))
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync(ct);

    public async Task<bool> ExistsAsync(string farmId, string supplierId, string invoiceNumber, CancellationToken ct = default)
    {
        var key = VendorInvoice.NormaliseNumber(invoiceNumber);
        return await _db.VendorInvoices.AnyAsync(v => v.FarmId == farmId && v.SupplierId == supplierId && v.InvoiceNumberKey == key, ct);
    }

    public async Task<VendorInvoice> AddAsync(VendorInvoice invoice, CancellationToken ct = default)
    {
        _db.VendorInvoices.Add(invoice);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            _db.Entry(invoice).State = EntityState.Detached;
            throw new DuplicateVendorInvoiceException();
        }
        return invoice;
    }

    public async Task UpdateAsync(VendorInvoice invoice, CancellationToken ct = default)
    {
        _db.VendorInvoices.Update(invoice);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(VendorInvoice invoice, CancellationToken ct = default)
    {
        _db.VendorInvoices.Remove(invoice);
        await _db.SaveChangesAsync(ct);
    }
}
