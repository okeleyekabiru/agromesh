using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class FarmRepository : IFarmRepository
{
    private readonly AgroMeshDbContext _db;

    public FarmRepository(AgroMeshDbContext db) => _db = db;

    public async Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Farms.FindAsync([id], ct);

    public async Task<Farm?> GetByIdWithSectionsAsync(string id, CancellationToken ct = default) =>
        await _db.Farms
            .Include(f => f.Sections).ThenInclude(s => s.Blocks)
            .Include(f => f.WorkerRoles)
            .Include(f => f.Workers).ThenInclude(w => w.Role)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task<List<Farm>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Farms.Include(f => f.Sections).OrderBy(f => f.CreatedAt).ToListAsync(ct);

    public async Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) =>
        await _db.Farms.Where(f => f.OwnerId == ownerId).ToListAsync(ct);

    public async Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) =>
        await _db.Farms.Where(f => f.CompanyId == companyId).Include(f => f.Sections).OrderBy(f => f.CreatedAt).ToListAsync(ct);

    public async Task<Farm> AddAsync(Farm farm, CancellationToken ct = default)
    {
        _db.Farms.Add(farm);
        await _db.SaveChangesAsync(ct);
        return farm;
    }

    public async Task UpdateAsync(Farm farm, CancellationToken ct = default)
    {
        _db.Farms.Update(farm);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
        await _db.Farms.AnyAsync(f => f.Id == id, ct);
}
