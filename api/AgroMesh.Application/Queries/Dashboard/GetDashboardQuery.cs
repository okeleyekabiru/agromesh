using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
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
    private readonly BlockCycleService _cycles;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IBlockRepository _blockRepo;

    public GetDashboardQueryHandler(
        IFarmRepository farmRepo,
        IActivityRepository activityRepo,
        ITransactionRepository transactionRepo,
        BlockCycleService cycles,
        IInventoryRepository inventoryRepo,
        IBlockRepository blockRepo)
    {
        _farmRepo = farmRepo;
        _activityRepo = activityRepo;
        _transactionRepo = transactionRepo;
        _cycles = cycles;
        _inventoryRepo = inventoryRepo;
        _blockRepo = blockRepo;
    }

    public async Task<DashboardDto> Handle(GetDashboardQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new DomainException($"Farm '{query.FarmId}' not found.");

        // Awaited one after another: these repositories share a single scoped DbContext,
        // which does not allow concurrent queries.
        var spend = await _transactionRepo.GetTotalSpendMtdAsync(query.FarmId, ct);
        var labour = await _activityRepo.GetLabourCostMtdAsync(query.FarmId, ct);
        var actCount = await _activityRepo.GetCountThisMonthAsync(query.FarmId, ct);
        var pending = await _transactionRepo.GetPendingReviewCountAsync(query.FarmId, ct);
        var recent = await _activityRepo.GetRecentByFarmIdAsync(query.FarmId, 5, ct);
        var lowStock = await _inventoryRepo.GetLowStockAsync(query.FarmId, ct);
        var blocks = await _blockRepo.GetByFarmIdAsync(query.FarmId, ct);
        var blockCosts = await _activityRepo.GetBlockCostsMtdAsync(query.FarmId, ct);
        var monthTxns = await _transactionRepo.GetMtdAsync(query.FarmId, ct);

        // Spend = money out of the bank plus the cost of logged activities, for the last 6 months.
        var now = DateTime.UtcNow;
        var trendStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5);
        var bankByMonth = await _transactionRepo.GetMonthlySpendAsync(query.FarmId, trendStart, ct);
        var activityByMonth = await _activityRepo.GetMonthlyCostsAsync(query.FarmId, trendStart, ct);
        var spendTrend = Enumerable.Range(0, 6).Select(i =>
        {
            var m = trendStart.AddMonths(i);
            var amount = bankByMonth.Concat(activityByMonth)
                .Where(x => x.Year == m.Year && x.Month == m.Month).Sum(x => x.Amount);
            return new MonthSpendDto(m.Year, m.Month, amount);
        }).ToList();

        var farm2 = farm; // needed for AreaUnit
        var recentActivities = recent.Select(a => new ActivityLogDto(
            a.Id, a.FarmId, a.BlockId, a.ActivityType, a.Date, a.Notes, a.SupervisorId,
            a.LabourCost, a.InputCost, a.CreatedOffline, a.SyncedAt, a.WorkedAreaGeoJson,
            a.WorkedHectares.HasValue ? AreaDto.From(a.WorkedHectares.Value, farm.PreferredAreaUnit) : null,
            a.ChemicalProductId, a.ChemicalProductName,
            a.Workers.Select(w => new WorkerLogDto(w.WorkerId, w.WorkerName, w.WorkerType, w.HoursWorked, w.PayRate, w.PayFrequency)).ToList(),
            a.Inputs.Select(i => new InputUsedDto(i.Item, i.Quantity, i.Unit)).ToList()
        )).ToList();

        var blockMap = blocks.ToDictionary(s => s.Id, s => s.Name);
        var today = DateTime.UtcNow.Date;

        // Upcoming work comes from the weather-based cycle of each block; blocks that need nothing yet
        // (e.g. no weeding in the dry season) or have no history are left off the list.
        var (cycles, weatherKnown) = await _cycles.ComputeAsync(farm, blocks, ct);
        var upcomingTasks = blocks
            .SelectMany(b => new[]
            {
                BlockCycleService.ToTask(b, ActivityType.Weeding, cycles[b.Id].Weeding, weatherKnown),
                BlockCycleService.ToTask(b, ActivityType.Spraying, cycles[b.Id].Spraying, weatherKnown),
                cycles[b.Id].Fertilizing is { } f
                    ? BlockCycleService.ToTask(b, ActivityType.FertilizerApplication, f, weatherKnown)
                    : BlockCycleService.ToTask(b, ActivityType.FertilizerApplication,
                        new CycleResult(CycleStatus.Unknown, null, null, "", [], Domain.Enums.ConfidenceLevel.Low), weatherKnown),
            })
            .Where(t => t.Status is "OnTrack" or "DueSoon" or "Overdue")
            .OrderBy(t => t.PredictedNextDue)
            .Take(5)
            .ToList();

        var lowStockAlerts = lowStock.Select(i => new LowStockAlertDto(i.Id, i.Name, i.CurrentStock, i.Unit, i.ReorderThreshold)).ToList();

        // Cost per block this month: labour + inputs from logged activities. Every block is listed
        // (zero if nothing was logged) so the dashboard never has to invent a row.
        var costByBlock = blockCosts.ToDictionary(c => c.BlockId);
        var blocksMtd = blocks.Select(b =>
        {
            var c = costByBlock.GetValueOrDefault(b.Id);
            return new BlockMtdDto(b.Id, b.Name,
                (c?.LabourCost ?? 0) + (c?.InputCost ?? 0), c?.ActivityCount ?? 0,
                c?.LabourCost ?? 0, c?.InputCost ?? 0, c?.MaintenanceCost ?? 0);
        }).OrderByDescending(b => b.Cost).ToList();

        var inputCost = blocksMtd.Sum(b => b.InputCost);
        var maintenanceCost = blocksMtd.Sum(b => b.MaintenanceCost);

        return new DashboardDto(
            query.FarmId,
            spend + labour + inputCost, labour, inputCost, maintenanceCost, actCount, pending,
            spendTrend, blocksMtd, BuildCropPnl(monthTxns), recentActivities, upcomingTasks, lowStockAlerts);
    }

    // Crop P&L from this month's bank transactions: each one is split across crops by its
    // CropAllocation percentages. Money in is revenue, money out is cost. Transactions that have
    // not been allocated to a crop yet are left out rather than guessed.
    public static List<CropPnlDto> BuildCropPnl(IEnumerable<Domain.Entities.Transaction> transactions)
    {
        var revenue = new Dictionary<string, decimal>();
        var cost = new Dictionary<string, decimal>();

        foreach (var t in transactions)
        {
            // The owner's own capital is not something the crops earned.
            if (t.Kind == Domain.Enums.TransactionKind.Capital) continue;
            var total = t.CropAllocation.Values.Sum();
            if (total <= 0) continue;
            foreach (var (crop, pct) in t.CropAllocation)
            {
                var share = Math.Abs(t.Amount) * (decimal)(pct / total);
                var bucket = t.Amount >= 0 ? revenue : cost;
                bucket[crop] = bucket.GetValueOrDefault(crop) + share;
            }
        }

        return revenue.Keys.Union(cost.Keys)
            .Select(crop =>
            {
                var r = revenue.GetValueOrDefault(crop);
                var c = cost.GetValueOrDefault(crop);
                return new CropPnlDto(crop, Math.Round(r, 2), Math.Round(c, 2), Math.Round(r - c, 2));
            })
            .OrderByDescending(p => p.Cost)
            .ToList();
    }
}
