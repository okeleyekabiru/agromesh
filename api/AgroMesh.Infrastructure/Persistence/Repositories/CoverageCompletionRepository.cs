using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class CoverageCompletionRepository : ICoverageCompletionRepository
{
    private readonly AgroMeshDbContext _db;

    public CoverageCompletionRepository(AgroMeshDbContext db) => _db = db;

    public async Task<List<CoverageCompletion>> GetByBlockAsync(string blockId, ActivityType type, CancellationToken ct = default) =>
        await _db.CoverageCompletions.Where(c => c.BlockId == blockId && c.ActivityType == type)
            .OrderBy(c => c.CompletedOn).ToListAsync(ct);

    public async Task AddAsync(CoverageCompletion completion, CancellationToken ct = default)
    {
        _db.CoverageCompletions.Add(completion);
        await _db.SaveChangesAsync(ct);
    }
}
