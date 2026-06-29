namespace AgroMesh.Application.DTOs;

public record CropPnlDto(string CropType, decimal Revenue, decimal Cost, decimal NetProfit);

public record SectionMtdDto(string SectionId, string SectionName, decimal Cost, int ActivityCount);

public record DashboardDto(
    string FarmId,
    decimal TotalSpendMtd,
    decimal LabourCostMtd,
    int ActivitiesThisMonth,
    int PendingReviewCount,
    List<SectionMtdDto> SectionsMtd,
    List<CropPnlDto> CropPnl,
    List<ActivityLogDto> RecentActivities,
    List<UpcomingTaskDto> UpcomingTasks,
    List<LowStockAlertDto> LowStockAlerts);

public record OnboardingStatusDto(
    bool HasFarmProfile,
    bool HasWorkerRoles,
    bool HasWorkers,
    bool HasSections,
    bool HasInventory,
    bool HasAllocationRules,
    bool IsComplete);
