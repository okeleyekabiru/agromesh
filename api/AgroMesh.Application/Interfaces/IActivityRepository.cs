using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface IActivityRepository
{
    Task<ActivityLog?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(List<ActivityLog> Items, int Total)> GetByFarmIdAsync(string farmId, int page, int pageSize, CancellationToken ct = default);
    Task<List<ActivityLog>> GetRecentBySectionAsync(string sectionId, ActivityType activityType, int limit = 5, CancellationToken ct = default);
    Task<List<ActivityLog>> GetRecentByFarmIdAsync(string farmId, int limit = 10, CancellationToken ct = default);
    Task<ActivityLog> AddAsync(ActivityLog activity, CancellationToken ct = default);
    Task<int> GetCountThisMonthAsync(string farmId, CancellationToken ct = default);
    Task<decimal> GetLabourCostMtdAsync(string farmId, CancellationToken ct = default);
}
