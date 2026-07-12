using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Dashboard;

public record GetDashboardQuery(string FarmId) : IRequest<DashboardDto>;

public class GetDashboardQueryHandler : IRequestHandler<GetDashboardQuery, DashboardDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IActivityRepository _activityRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly ISchedulingPredictionRepository _predictionRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ISectionRepository _sectionRepo;

    public GetDashboardQueryHandler(
        IFarmRepository farmRepo,
        IActivityRepository activityRepo,
        ITransactionRepository transactionRepo,
        ISchedulingPredictionRepository predictionRepo,
        IInventoryRepository inventoryRepo,
        ISectionRepository sectionRepo)
    {
        _farmRepo = farmRepo;
        _activityRepo = activityRepo;
        _transactionRepo = transactionRepo;
        _predictionRepo = predictionRepo;
        _inventoryRepo = inventoryRepo;
        _sectionRepo = sectionRepo;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new DomainException($"Farm '{query.FarmId}' not found.");

        var spendTask = _transactionRepo.GetTotalSpendMtdAsync(query.FarmId, ct);
        var labourTask = _activityRepo.GetLabourCostMtdAsync(query.FarmId, ct);
        var actCountTask = _activityRepo.GetCountThisMonthAsync(query.FarmId, ct);
        var pendingTask = _transactionRepo.GetPendingReviewCountAsync(query.FarmId, ct);
        var recentTask = _activityRepo.GetRecentByFarmIdAsync(query.FarmId, 5, ct);
        var predictionsTask = _predictionRepo.GetByFarmIdAsync(query.FarmId, ct);
        var lowStockTask = _inventoryRepo.GetLowStockAsync(query.FarmId, ct);
        var sectionsTask = _sectionRepo.GetByFarmIdAsync(query.FarmId, ct);

        await Task.WhenAll(spendTask, labourTask, actCountTask, pendingTask, recentTask, predictionsTask, lowStockTask, sectionsTask);

        var farm2 = farm; // needed for AreaUnit
        var recentActivities = (await recentTask).Select(a => new ActivityLogDto(
            a.Id, a.FarmId, a.SectionId, a.BlockId, a.ActivityType, a.Date, a.Notes, a.SupervisorId,
            a.LabourCost, a.InputCost, a.CreatedOffline, a.SyncedAt, a.WorkedAreaGeoJson,
            a.WorkedHectares.HasValue ? AreaDto.From(a.WorkedHectares.Value, farm.PreferredAreaUnit) : null,
            a.ChemicalProductId, a.ChemicalProductName,
            a.Workers.Select(w => new WorkerLogDto(w.WorkerId, w.WorkerName, w.WorkerType, w.HoursWorked, w.PayRate, w.PayFrequency)).ToList(),
            a.Inputs.Select(i => new InputUsedDto(i.Item, i.Quantity, i.Unit)).ToList()
        )).ToList();

        var sections = await sectionsTask;
        var sectionMap = sections.ToDictionary(s => s.Id, s => s.Name);
        var today = DateTime.UtcNow.Date;

        var predictions = await predictionsTask;
        var upcomingTasks = predictions
            .OrderBy(p => p.PredictedNextDue)
            .Take(5)
            .Select(p => new UpcomingTaskDto(
                p.SectionId, sectionMap.GetValueOrDefault(p.SectionId, p.SectionId),
                p.ActivityType, p.LastActivityDate, p.PredictedNextDue,
                (p.PredictedNextDue.Date - today).Days,
                p.Reasoning, p.RecommendedChemical, p.RecommendedSprayDate,
                p.Confidence, null, null, p.IsStale, p.WeatherDataUsed))
            .ToList();

        var lowStock = await lowStockTask;
        var lowStockAlerts = lowStock.Select(i => new LowStockAlertDto(i.Id, i.Name, i.CurrentStock, i.Unit, i.ReorderThreshold)).ToList();

        return new DashboardDto(
            query.FarmId,
            await spendTask, await labourTask, await actCountTask, await pendingTask,
            [], [], recentActivities, upcomingTasks, lowStockAlerts);
    }
}
