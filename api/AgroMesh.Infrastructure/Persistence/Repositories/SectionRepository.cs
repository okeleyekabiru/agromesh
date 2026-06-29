using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class SectionRepository : ISectionRepository
{
    private readonly AgroMeshDbContext _db;

    public SectionRepository(AgroMeshDbContext db) => _db = db;

    public async Task<Section?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Sections.FindAsync([id], ct);

    public async Task<Section?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) =>
        await _db.Sections.Include(s => s.Blocks).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<Section>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.Sections.Where(s => s.FarmId == farmId).Include(s => s.Blocks).ToListAsync(ct);

    public async Task<Section> AddAsync(Section section, CancellationToken ct = default)
    {
        _db.Sections.Add(section);
        await _db.SaveChangesAsync(ct);
        return section;
    }

    public async Task UpdateAsync(Section section, CancellationToken ct = default)
    {
        _db.Sections.Update(section);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
        await _db.Sections.AnyAsync(s => s.Id == id, ct);
}
