using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly AgroMeshDbContext _db;

    public BlockRepository(AgroMeshDbContext db) => _db = db;

    public async Task<Block?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Blocks.FindAsync([id], ct);

    public async Task<List<Block>> GetBySectionIdAsync(string sectionId, CancellationToken ct = default) =>
        await _db.Blocks.Where(b => b.SectionId == sectionId).ToListAsync(ct);

    public async Task<List<Block>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.Blocks.Where(b => b.FarmId == farmId).ToListAsync(ct);

    public async Task<Block> AddAsync(Block block, CancellationToken ct = default)
    {
        _db.Blocks.Add(block);
        await _db.SaveChangesAsync(ct);
        return block;
    }

    public async Task UpdateAsync(Block block, CancellationToken ct = default)
    {
        _db.Blocks.Update(block);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
        await _db.Blocks.AnyAsync(b => b.Id == id, ct);
}
