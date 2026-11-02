using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class ChemicalProductRepository : IChemicalProductRepository
{
    private readonly IlevoraDbContext _db;

    public ChemicalProductRepository(IlevoraDbContext db) => _db = db;

    public async Task<ChemicalProduct?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.ChemicalProducts.FindAsync([id], ct);

    public async Task<List<ChemicalProduct>> GetAllActiveAsync(CancellationToken ct = default) =>
        await _db.ChemicalProducts.Where(c => c.Active).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<ChemicalProduct> AddAsync(ChemicalProduct product, CancellationToken ct = default)
    {
        _db.ChemicalProducts.Add(product);
        await _db.SaveChangesAsync(ct);
        return product;
    }

    public async Task UpdateAsync(ChemicalProduct product, CancellationToken ct = default)
    {
        _db.ChemicalProducts.Update(product);
        await _db.SaveChangesAsync(ct);
    }
}
