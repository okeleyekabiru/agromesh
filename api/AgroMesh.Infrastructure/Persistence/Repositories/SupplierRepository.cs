using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class SupplierRepository : ISupplierRepository
{
    private readonly AgroMeshDbContext _db;

    public SupplierRepository(AgroMeshDbContext db) => _db = db;

    public async Task<Supplier?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Suppliers.FindAsync([id], ct);

    public async Task<List<Supplier>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.Suppliers
            .Where(s => s.FarmId == farmId)
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

    public async Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default)
    {
        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync(ct);
        return supplier;
    }
}
