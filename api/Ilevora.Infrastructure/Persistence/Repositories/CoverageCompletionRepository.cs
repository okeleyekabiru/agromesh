using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class CoverageCompletionRepository : ICoverageCompletionRepository
{
    private readonly IlevoraDbContext _db;

    public CoverageCompletionRepository(IlevoraDbContext db) => _db = db;

    public async Task<List<CoverageCompletion>> GetByBlockAsync(string blockId, ActivityType type, CancellationToken ct = default) =>
        await _db.CoverageCompletions.Where(c => c.BlockId == blockId && c.ActivityType == type)
            .OrderBy(c => c.CompletedOn).ToListAsync(ct);

    public async Task AddAsync(CoverageCompletion completion, CancellationToken ct = default)
    {
        _db.CoverageCompletions.Add(completion);
        await _db.SaveChangesAsync(ct);
    }
}
