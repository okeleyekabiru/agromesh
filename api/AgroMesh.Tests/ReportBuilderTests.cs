using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Report;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class ReportBuilderTests
{
    private static InventoryItem Item(string name, string category, decimal unitCost)
    {
        var item = InventoryItem.Create("f1", name, "kg", "cat", 100, 1);
        item.SetUnitCost(unitCost);
        typeof(InventoryItem).GetProperty(nameof(InventoryItem.Category))!
            .SetValue(item, InventoryCategory.Create("f1", category));
        return item;
    }

    private static ActivityLog Activity(string blockId, ActivityType type, decimal labour, decimal inputCost, params InputUsed[] inputs)
    {
        var log = ActivityLog.Create("f1", blockId, type, DateTime.UtcNow, [], inputs.ToList(), "sup");
        typeof(ActivityLog).GetProperty(nameof(ActivityLog.LabourCost))!.SetValue(log, labour);
        log.SetInputCost(inputCost);
        return log;
    }

    private static Transaction Txn(decimal amount, TransactionCategory cat) =>
        Transaction.Create("f1", Guid.NewGuid().ToString(), "n", amount, DateTime.UtcNow, cat, ConfidenceLevel.High, CategorizationSource.Manual);

    [Fact]
    public void Totals_split_into_categories_that_add_up()
    {
        var npk = Item("NPK", "Fertilizer", 500);
        var fuel = Item("Diesel", "Fuel", 1000);
        var block = Block.Create("f1", "Alpha", [new BlockCrop(CropType.OilPalm, 10, 100)], 10);

        var report = GetReportQueryHandler.Build(2026, 10,
            [Activity(block.Id, ActivityType.FertilizerApplication, 4000, 30000,
                new InputUsed("NPK", 40, "kg", npk.Id), new InputUsed("Diesel", 10, "kg", fuel.Id))],
            [Txn(-2000, TransactionCategory.Equipment), Txn(5000, TransactionCategory.Sales)],
            [block], [npk, fuel], []);

        // NPK is 20,000 of the 30,000 priced inputs, diesel 10,000: split 2:1 across the 30,000 saved.
        Assert.Equal(36000m, report.TotalExpenditure);
        Assert.Equal(4000m, report.LabourCost);
        Assert.Equal(30000m, report.InputCost);
        Assert.Equal(2000m, report.BankSpend);
        Assert.Equal(report.TotalExpenditure, report.Categories.Sum(c => c.Amount));
        Assert.Equal(20000m, report.Categories.Single(c => c.Label == GetReportQueryHandler.FertChem).Amount);
        Assert.Equal(10000m, report.Categories.Single(c => c.Label == GetReportQueryHandler.FuelTransport).Amount);
        Assert.Equal(2000m, report.Categories.Single(c => c.Label == GetReportQueryHandler.EquipMaint).Amount);
    }

    [Fact]
    public void Cost_per_hectare_uses_the_blocks_activity_cost()
    {
        var a = Block.Create("f1", "Alpha", [new BlockCrop(CropType.OilPalm, 10, 100)], 10);
        var b = Block.Create("f1", "Beta", [new BlockCrop(CropType.Maize, 4, 100)], 4);

        var report = GetReportQueryHandler.Build(2026, 10,
            [Activity(a.Id, ActivityType.Weeding, 5000, 0)], [], [a, b], [], []);

        var alpha = report.Blocks.Single(x => x.BlockName == "Alpha");
        Assert.Equal(500m, alpha.CostPerArea);
        Assert.Equal("ha", report.AreaUnit);
        Assert.Equal("OilPalm", alpha.CropType);
        Assert.Equal(0m, report.Blocks.Single(x => x.BlockName == "Beta").Cost);
        // No area entered -> no per-area figure rather than a misleading 0.
        var noArea = GetReportQueryHandler.Build(2026, 10, [Activity(a.Id, ActivityType.Weeding, 100, 0)], [], [Block.Create("f1", "Gamma", [new BlockCrop(CropType.OilPalm, 0, 100)], 0)], [], []);
        Assert.Null(noArea.Blocks.Single().CostPerArea);
        Assert.Equal("Weeding", report.ByActivityType.Single().ActivityType);
    }

    [Fact]
    public void Cost_per_area_follows_the_farms_unit_in_acres()
    {
        var a = Block.Create("f1", "Alpha", [new BlockCrop(CropType.OilPalm, 10, 100)], 10); // 24.71 ac
        var report = GetReportQueryHandler.Build(2026, 10, [Activity(a.Id, ActivityType.Weeding, 24710, 0)], [], [a], [], [], AreaUnit.Acres);

        Assert.Equal("ac", report.AreaUnit);
        Assert.Equal(24.71, report.Blocks.Single().Area);
        Assert.Equal(1000m, report.Blocks.Single().CostPerArea);
    }

    [Fact]
    public void An_empty_month_has_no_categories()
    {
        var report = GetReportQueryHandler.Build(2026, 9, [], [], [], [], []);
        Assert.Empty(report.Categories);
        Assert.Equal(0m, report.TotalExpenditure);
    }
}

public class SprayDayTests
{
    [Theory]
    [InlineData(0.0, 10.0, 10, true)]
    [InlineData(5.0, 10.0, 10, false)]
    [InlineData(0.0, 30.0, 10, false)]
    [InlineData(0.0, 10.0, 80, false)]
    public void Good_spray_day_needs_dry_calm_and_low_chance_of_rain(double mm, double wind, int chance, bool good) =>
        Assert.Equal(good, AgroMesh.Application.Queries.Farm.GetWeatherQueryHandler.IsGoodSprayDay(
            new AgroMesh.Application.Interfaces.DailyWeather(DateTime.UtcNow, mm, 30, 22, wind, 0, chance)));
}
