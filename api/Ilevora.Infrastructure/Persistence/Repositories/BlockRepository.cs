using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly IlevoraDbContext _db;

    public BlockRepository(IlevoraDbContext db) => _db = db;

    public async Task<Block?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Blocks.FindAsync([id], ct);

    public async Task<List<Block>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.Blocks.Where(s => s.FarmId == farmId).ToListAsync(ct);

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
        await _db.Blocks.AnyAsync(s => s.Id == id, ct);
}
