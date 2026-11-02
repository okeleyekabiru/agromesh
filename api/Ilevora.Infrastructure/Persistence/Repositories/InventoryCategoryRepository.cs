using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class InventoryCategoryRepository : IInventoryCategoryRepository
{
    private readonly IlevoraDbContext _db;

    public InventoryCategoryRepository(IlevoraDbContext db) => _db = db;

    public async Task<InventoryCategory?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.InventoryCategories.FindAsync([id], ct);

    public async Task<List<InventoryCategory>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.InventoryCategories
            .Where(c => c.FarmId == farmId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

    public async Task<InventoryCategory?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) =>
        await _db.InventoryCategories
            .FirstOrDefaultAsync(c => c.FarmId == farmId && c.Name.ToLower() == name.ToLower(), ct);

    public async Task<InventoryCategory> AddAsync(InventoryCategory category, CancellationToken ct = default)
    {
        _db.InventoryCategories.Add(category);
        await _db.SaveChangesAsync(ct);
        return category;
    }

    public async Task AddRangeAsync(IEnumerable<InventoryCategory> categories, CancellationToken ct = default)
    {
        _db.InventoryCategories.AddRange(categories);
        await _db.SaveChangesAsync(ct);
    }
}
