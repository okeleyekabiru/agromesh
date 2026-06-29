using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class CropAllocationRuleRepository : ICropAllocationRuleRepository
{
    private readonly AgroMeshDbContext _db;

    public CropAllocationRuleRepository(AgroMeshDbContext db) => _db = db;

    public async Task<List<CropAllocationRule>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.CropAllocationRules.Where(r => r.FarmId == farmId).ToListAsync(ct);

    public async Task AddBulkAsync(List<CropAllocationRule> rules, CancellationToken ct = default)
    {
        _db.CropAllocationRules.AddRange(rules);
        await _db.SaveChangesAsync(ct);
    }
}
