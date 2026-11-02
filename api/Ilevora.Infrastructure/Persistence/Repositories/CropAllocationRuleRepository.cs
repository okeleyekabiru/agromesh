using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class CropAllocationRuleRepository : ICropAllocationRuleRepository
{
    private readonly IlevoraDbContext _db;

    public CropAllocationRuleRepository(IlevoraDbContext db) => _db = db;

    public async Task<List<CropAllocationRule>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.CropAllocationRules.Where(r => r.FarmId == farmId).ToListAsync(ct);

    public async Task AddBulkAsync(List<CropAllocationRule> rules, CancellationToken ct = default)
    {
        _db.CropAllocationRules.AddRange(rules);
        await _db.SaveChangesAsync(ct);
    }
}
