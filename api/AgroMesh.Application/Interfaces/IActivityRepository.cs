using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface IActivityRepository
{
    Task<ActivityLog?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<(List<ActivityLog> Items, int Total)> GetByFarmIdAsync(string farmId, int page, int pageSize, CancellationToken ct = default);
    Task<List<ActivityLog>> GetRecentByBlockAsync(string blockId, ActivityType activityType, int limit = 5, CancellationToken ct = default);
    Task<List<ActivityLog>> GetRecentByFarmIdAsync(string farmId, int limit = 10, CancellationToken ct = default);
    Task<ActivityLog> AddAsync(ActivityLog activity, CancellationToken ct = default);
    /// <summary>All of a farm's activities dated in [from, to).</summary>
    Task<List<ActivityLog>> GetBetweenAsync(string farmId, DateTime from, DateTime to, CancellationToken ct = default);
    Task UpdateAsync(ActivityLog activity, CancellationToken ct = default);
    /// <summary>Activities with no input cost yet whose inputs include the given stock item.</summary>
    Task<List<ActivityLog>> GetUnpricedUsingItemAsync(string farmId, string inventoryItemId, CancellationToken ct = default);
    Task<int> GetCountThisMonthAsync(string farmId, CancellationToken ct = default);
    Task<decimal> GetLabourCostMtdAsync(string farmId, CancellationToken ct = default);
    Task<List<MonthAmount>> GetMonthlyCostsAsync(string farmId, DateTime from, CancellationToken ct = default);
    Task<List<BlockCostRow>> GetBlockCostsMtdAsync(string farmId, CancellationToken ct = default);
}

/// <summary>This month's activity count and costs for one block (MaintenanceCost is the part from Maintenance activities).</summary>
public record BlockCostRow(string BlockId, int ActivityCount, decimal LabourCost, decimal InputCost, decimal MaintenanceCost);

/// <summary>An amount of money for one calendar month.</summary>
public record MonthAmount(int Year, int Month, decimal Amount);
