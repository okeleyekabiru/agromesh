using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class InvoiceRepository : IInvoiceRepository
{
    private readonly IlevoraDbContext _db;

    public InvoiceRepository(IlevoraDbContext db) => _db = db;

    public async Task<Invoice?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Invoices.FindAsync([id], ct);

    public async Task<List<Invoice>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.Invoices.Where(i => i.FarmId == farmId).OrderByDescending(i => i.Number).ToListAsync(ct);

    public async Task<int> ReserveNextNumberAsync(string farmId, CancellationToken ct = default)
    {
        // One statement, atomic under any concurrency: the first call for a farm inserts 1, later calls add 1 to the stored value
        // (the migration seeded existing farms with their highest number). Row-locked, so parallel callers get distinct numbers.
        var numbers = await _db.Database
            .SqlQuery<int>($"""
                INSERT INTO "InvoiceCounters" ("FarmId", "LastNumber") VALUES ({farmId}, 1)
                ON CONFLICT ("FarmId") DO UPDATE SET "LastNumber" = "InvoiceCounters"."LastNumber" + 1
                RETURNING "LastNumber" AS "Value"
                """)
            .ToListAsync(ct);
        return numbers.Single();
    }

    public async Task<Invoice> AddAsync(Invoice invoice, CancellationToken ct = default)
    {
        _db.Invoices.Add(invoice);
        await _db.SaveChangesAsync(ct);
        return invoice;
    }

    public async Task UpdateAsync(Invoice invoice, CancellationToken ct = default)
    {
        _db.Invoices.Update(invoice);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Invoice invoice, CancellationToken ct = default)
    {
        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync(ct);
    }

}
