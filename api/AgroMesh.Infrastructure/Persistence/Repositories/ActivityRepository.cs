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

    public async Task<List<ActivityLog>> GetRecentBySectionAsync(
        string sectionId, ActivityType activityType, int limit = 5, CancellationToken ct = default) =>
        await _db.ActivityLogs
            .Where(a => a.SectionId == sectionId && a.ActivityType == activityType)
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

    public async Task<int> GetCountThisMonthAsync(string farmId, CancellationToken ct = default)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _db.ActivityLogs.CountAsync(a => a.FarmId == farmId && a.Date >= start, ct);
    }

    public async Task<decimal> GetLabourCostMtdAsync(string farmId, CancellationToken ct = default)
    {
        var start = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        return await _db.ActivityLogs
            .Where(a => a.FarmId == farmId && a.Date >= start)
            .SumAsync(a => a.LabourCost, ct);
    }
}
