using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class CropRepository : ICropRepository
{
    private readonly IlevoraDbContext _db;

    public CropRepository(IlevoraDbContext db) => _db = db;

    public async Task<Crop?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Crops.FindAsync([id], ct);

    public async Task<List<Crop>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.Crops.Where(c => c.FarmId == farmId).OrderBy(c => c.Name).ToListAsync(ct);

    public async Task<Crop> AddAsync(Crop crop, CancellationToken ct = default)
    {
        _db.Crops.Add(crop);
        await _db.SaveChangesAsync(ct);
        return crop;
    }
}
