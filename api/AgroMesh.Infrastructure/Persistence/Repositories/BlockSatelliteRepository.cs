using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class BlockSatelliteRepository : IBlockSatelliteRepository
{
    private readonly AgroMeshDbContext _db;

    public BlockSatelliteRepository(AgroMeshDbContext db) => _db = db;

    public Task<List<BlockSatelliteReading>> GetByFarmAsync(string farmId, DateTime since, CancellationToken ct = default) =>
        _db.BlockSatelliteReadings.AsNoTracking().Where(r => r.FarmId == farmId && r.Date >= since).ToListAsync(ct);

    public Task<List<BlockSatelliteReading>> GetByBlockAsync(string blockId, DateTime since, CancellationToken ct = default) =>
        _db.BlockSatelliteReadings.AsNoTracking().Where(r => r.BlockId == blockId && r.Date >= since).OrderBy(r => r.Date).ToListAsync(ct);

    public async Task UpsertAsync(string farmId, string blockId, IEnumerable<SatelliteObservation> observations, CancellationToken ct = default)
    {
        var list = observations.ToList();
        if (list.Count == 0) return;
        var from = list.Min(o => o.From).Date;
        var existing = await _db.BlockSatelliteReadings.Where(r => r.BlockId == blockId && r.Date >= from).ToListAsync(ct);

        foreach (var o in list)
        {
            var date = DateTime.SpecifyKind(o.From.Date, DateTimeKind.Utc);
            var row = existing.FirstOrDefault(r => r.Date == date);
            if (row is null) _db.BlockSatelliteReadings.Add(BlockSatelliteReading.Create(farmId, blockId, date, o.Ndvi, o.Ndmi, o.ValidShare));
            else row.Update(o.Ndvi, o.Ndmi, o.ValidShare);
        }
        await _db.SaveChangesAsync(ct);
    }
}
