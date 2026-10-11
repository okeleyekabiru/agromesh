namespace AgroMesh.Application.DTOs;

public record CropPnlDto(string CropType, decimal Revenue, decimal Cost, decimal NetProfit);

public record BlockMtdDto(string BlockId, string BlockName, decimal Cost, int ActivityCount, decimal LabourCost, decimal InputCost, decimal MaintenanceCost);

public record MonthSpendDto(int Year, int Month, decimal Amount);

public record DashboardDto(
    string FarmId,
    decimal TotalSpendMtd,
    decimal LabourCostMtd,
    decimal InputCostMtd,
    decimal MaintenanceCostMtd,
    int ActivitiesThisMonth,
    int PendingReviewCount,
    List<MonthSpendDto> SpendTrend,
    List<BlockMtdDto> BlocksMtd,
    List<CropPnlDto> CropPnl,
    List<ActivityLogDto> RecentActivities,
    List<UpcomingTaskDto> UpcomingTasks,
    List<LowStockAlertDto> LowStockAlerts);

public record OnboardingStatusDto(
    bool HasFarmProfile,
    bool HasWorkerRoles,
    bool HasWorkers,
    bool HasBlocks,
    bool HasInventory,
    bool HasAllocationRules,
    bool IsComplete);
