using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using MediatR;

namespace AgroMesh.Application.Queries.Report;

public record GetReportQuery(string FarmId, int? Year = null, int? Month = null) : IRequest<ReportDto>;

public class GetReportQueryHandler : IRequestHandler<GetReportQuery, ReportDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IActivityRepository _activityRepo;
    private readonly ITransactionRepository _transactionRepo;
    private readonly IBlockRepository _blockRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public GetReportQueryHandler(
        IFarmRepository farmRepo, IActivityRepository activityRepo, ITransactionRepository transactionRepo,
        IBlockRepository blockRepo, IInventoryRepository inventoryRepo)
    {
        _farmRepo = farmRepo;
        _activityRepo = activityRepo;
        _transactionRepo = transactionRepo;
        _blockRepo = blockRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<ReportDto> Handle(GetReportQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new DomainException($"Farm '{query.FarmId}' not found.");

        var now = DateTime.UtcNow;
        var year = query.Year ?? now.Year;
        var month = query.Month ?? now.Month;
        if (month is < 1 or > 12 || year is < 2000 or > 2100)
            throw new DomainException("Choose a valid month.");

        var from = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = from.AddMonths(1);

        // Awaited one by one: the repositories share a single DbContext.
        var activities = await _activityRepo.GetBetweenAsync(query.FarmId, from, to, ct);
        var transactions = await _transactionRepo.GetBetweenAsync(query.FarmId, from, to, ct);
        var blocks = await _blockRepo.GetByFarmIdAsync(query.FarmId, ct);
        var items = await _inventoryRepo.GetByFarmIdAsync(query.FarmId, ct);

        var trendStart = from.AddMonths(-5);
        var bankByMonth = await _transactionRepo.GetMonthlySpendAsync(query.FarmId, trendStart, ct);
        var activityByMonth = await _activityRepo.GetMonthlyCostsAsync(query.FarmId, trendStart, ct);
        var trend = Enumerable.Range(0, 6).Select(i =>
        {
            var m = trendStart.AddMonths(i);
            var amount = bankByMonth.Concat(activityByMonth).Where(x => x.Year == m.Year && x.Month == m.Month).Sum(x => x.Amount);
            return new MonthSpendDto(m.Year, m.Month, amount);
        }).ToList();

        return Build(year, month, activities, transactions, blocks, items, trend, farm.PreferredAreaUnit);
    }

    public const string Labour = "Labour";
    public const string FertChem = "Fertilizer & Chemicals";
    public const string FuelTransport = "Fuel & Transport";
    public const string EquipMaint = "Equipment & Maintenance";
    public const string Other = "Seeds & Other";
    private static readonly string[] Order = [Labour, FertChem, FuelTransport, EquipMaint, Other];

    private static string StockGroup(string? category) => (category ?? "").Trim().ToLowerInvariant() switch
    {
        "fertilizer" or "chemical" => FertChem,
        "fuel" => FuelTransport,
        "equipment" or "spare parts" or "farm tools" => EquipMaint,
        _ => Other,
    };

    private static string TransactionGroup(TransactionCategory c) => c switch
    {
        TransactionCategory.Labour => Labour,
        TransactionCategory.Fertilizer or TransactionCategory.Chemicals => FertChem,
        TransactionCategory.Fuel or TransactionCategory.Transportation => FuelTransport,
        TransactionCategory.Equipment or TransactionCategory.Maintenance => EquipMaint,
        _ => Other,
    };

    public static ReportDto Build(
        int year, int month,
        IReadOnlyList<ActivityLog> activities, IReadOnlyList<Domain.Entities.Transaction> transactions,
        IReadOnlyList<Domain.Entities.Block> blocks, IReadOnlyList<InventoryItem> items, List<MonthSpendDto> trend,
        AreaUnit areaUnit = AreaUnit.Hectares)
    {
        var byCategory = Order.ToDictionary(c => c, _ => 0m);
        var itemById = items.ToDictionary(i => i.Id);

        // Money out of the bank, by its category. A payment for stock (matched to an order) is left out: the stock's cost
        // is counted below, when the activities use it.
        var bankSpend = 0m;
        foreach (var t in transactions.Where(t => t.Amount < 0 && !t.IsStockPurchase))
        {
            var amount = -t.Amount;
            bankSpend += amount;
            byCategory[TransactionGroup(t.Category)] += amount;
        }

        // Logged activities: labour as is; the input cost is a single saved total, so it is spread over the
        // inputs by quantity x current price to find which stock categories it went to.
        var labour = 0m;
        var inputs = 0m;
        foreach (var a in activities)
        {
            labour += a.LabourCost;
            inputs += a.InputCost;
            byCategory[Labour] += a.LabourCost;
            if (a.InputCost <= 0) continue;

            var weights = a.Inputs.Select(i =>
            {
                var item = i.InventoryItemId is not null && itemById.TryGetValue(i.InventoryItemId, out var it) ? it : null;
                return (Group: StockGroup(item?.Category?.Name), Weight: (decimal)i.Quantity * (item?.UnitCost ?? 0));
            }).ToList();
            var totalWeight = weights.Sum(w => w.Weight);
            if (totalWeight <= 0)
            {
                byCategory[Other] += a.InputCost;
                continue;
            }
            foreach (var w in weights.Where(w => w.Weight > 0))
                byCategory[w.Group] += a.InputCost * (w.Weight / totalWeight);
        }

        var total = byCategory.Values.Sum();
        var categories = Order
            .Where(c => byCategory[c] > 0)
            .Select(c => new ReportCategoryDto(c, Math.Round(byCategory[c], 2), total > 0 ? Math.Round((double)(byCategory[c] / total) * 100, 1) : 0))
            .ToList();

        var costByBlock = activities.GroupBy(a => a.BlockId).ToDictionary(
            g => g.Key, g => (Cost: g.Sum(a => a.LabourCost + a.InputCost), Count: g.Count()));
        var blockRows = blocks.Select(b =>
        {
            var (cost, count) = costByBlock.GetValueOrDefault(b.Id);
            // Cost per area in the farm's own unit (acres or hectares).
            var area = AreaConverter.ToDisplayUnit(b.Hectares, areaUnit);
            return new ReportBlockDto(b.Id, b.Name, b.Crops.FirstOrDefault()?.CropType.ToString(), area,
                Math.Round(cost, 2), area > 0 ? Math.Round(cost / (decimal)area, 2) : null, count);
        }).OrderByDescending(b => b.Cost).ToList();

        var byType = activities.GroupBy(a => a.ActivityType)
            .Select(g => new ReportActivityTypeDto(g.Key.ToString(), g.Count(), Math.Round(g.Sum(a => a.LabourCost + a.InputCost), 2)))
            .OrderByDescending(t => t.Cost).ThenByDescending(t => t.Count).ToList();

        return new ReportDto(year, month, AreaConverter.UnitLabel(areaUnit), Math.Round(total, 2), Math.Round(labour, 2), Math.Round(inputs, 2),
            Math.Round(bankSpend, 2), activities.Count, categories, blockRows, byType, trend);
    }
}
