namespace AgroMesh.Application.DTOs;

public record ReportCategoryDto(string Label, decimal Amount, double Percent);

public record ReportBlockDto(
    string BlockId, string BlockName, string? CropType, double Area,
    decimal Cost, decimal? CostPerArea, int ActivityCount);

public record ReportActivityTypeDto(string ActivityType, int Count, decimal Cost);

/// <summary>
/// One month of the farm's costs. Spend = bank money out + the cost of logged activities (labour and
/// stock used), the same definition the dashboard uses.
/// </summary>
public record ReportDto(
    int Year,
    int Month,
    string AreaUnit,
    decimal TotalExpenditure,
    decimal LabourCost,
    decimal InputCost,
    decimal BankSpend,
    int ActivityCount,
    List<ReportCategoryDto> Categories,
    List<ReportBlockDto> Blocks,
    List<ReportActivityTypeDto> ByActivityType,
    List<MonthSpendDto> SpendTrend);
