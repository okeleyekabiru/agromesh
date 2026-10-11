using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class ActivityRepository : IActivityRepository
{
    private readonly AgroMeshDbContext _db;

    public ActivityRepository(AgroMeshDbContext db) => _db = db;

    public async Task<ActivityLog?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.ActivityLogs.FindAsync([id], ct);

    public async Task<(List<ActivityLog> Items, int Total)> GetByFarmIdAsync(
        string farmId, int page, int pageSize, CancellationToken ct = default)
    {
        var query = _db.ActivityLogs
            .Where(a => a.FarmId == farmId)
            .OrderByDescending(a => a.Date);

        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<List<ActivityLog>> GetRecentByBlockAsync(
        string blockId, ActivityType activityType, int limit = 5, CancellationToken ct = default) =>
        await _db.ActivityLogs
            .Where(a => a.BlockId == blockId && a.ActivityType == activityType)
            .OrderByDescending(a => a.Date)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<List<ActivityLog>> GetRecentByFarmIdAsync(
        string farmId, int limit = 10, CancellationToken ct = default) =>
        await _db.ActivityLogs
            .Where(a => a.FarmId == farmId)
            .OrderByDescending(a => a.Date)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<ActivityLog> AddAsync(ActivityLog activity, CancellationToken ct = default)
    {
        _db.ActivityLogs.Add(activity);
        await _db.SaveChangesAsync(ct);
        return activity;
    }

    public async Task<List<ActivityLog>> GetBetweenAsync(string farmId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _db.ActivityLogs.Where(a => a.FarmId == farmId && a.Date >= from && a.Date < to).ToListAsync(ct);

    public async Task UpdateAsync(ActivityLog activity, CancellationToken ct = default)
    {
        _db.ActivityLogs.Update(activity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<ActivityLog>> GetUnpricedUsingItemAsync(string farmId, string inventoryItemId, CancellationToken ct = default)
    {
        // Inputs are stored as JSON, so the item match is done in memory on the (few) unpriced rows.
        var unpriced = await _db.ActivityLogs.Where(a => a.FarmId == farmId && a.InputCost == 0).ToListAsync(ct);
        return unpriced.Where(a => a.Inputs.Any(i => i.InventoryItemId == inventoryItemId)).ToList();
    }

    public async Task<int> GetCountThisMonthAsync(string farmId, CancellationToken ct = default)
    {
        var start = MonthStart();
        return await _db.ActivityLogs.CountAsync(a => a.FarmId == farmId && a.Date >= start, ct);
    }

    public async Task<List<MonthAmount>> GetMonthlyCostsAsync(string farmId, DateTime from, CancellationToken ct = default) =>
        await _db.ActivityLogs
            .Where(a => a.FarmId == farmId && a.Date >= from)
            .GroupBy(a => new { a.Date.Year, a.Date.Month })
            .Select(g => new MonthAmount(g.Key.Year, g.Key.Month, g.Sum(a => a.LabourCost + a.InputCost)))
            .ToListAsync(ct);

    public async Task<List<BlockCostRow>> GetBlockCostsMtdAsync(string farmId, CancellationToken ct = default)
    {
        var start = MonthStart();
        return await _db.ActivityLogs
            .Where(a => a.FarmId == farmId && a.Date >= start)
            .GroupBy(a => a.BlockId)
            .Select(g => new BlockCostRow(
                g.Key,
                g.Count(),
                g.Sum(a => a.LabourCost),
                g.Sum(a => a.InputCost),
                g.Where(a => a.ActivityType == ActivityType.Maintenance).Sum(a => a.LabourCost + a.InputCost)))
            .ToListAsync(ct);
    }

    private static DateTime MonthStart()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public async Task<decimal> GetLabourCostMtdAsync(string farmId, CancellationToken ct = default)
    {
        var start = MonthStart();
        return await _db.ActivityLogs
            .Where(a => a.FarmId == farmId && a.Date >= start)
            .SumAsync(a => a.LabourCost, ct);
    }
}
