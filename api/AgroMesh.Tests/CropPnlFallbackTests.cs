using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Dashboard;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class CropPnlFallbackTests
{
    private static Block MakeBlock(string name, params (CropType Crop, double Ha, int Pct)[] crops) =>
        Block.Create("f1", name, crops.Select(c => new BlockCrop(c.Crop, c.Ha, c.Pct)).ToList(), crops.Sum(c => c.Ha));

    private static Transaction Txn(decimal amount, Dictionary<string, double>? alloc = null, TransactionKind? asKind = null,
                                   string? blockIdFor = null, string narration = "n")
    {
        var t = Transaction.Create("f1", Guid.NewGuid().ToString(), narration, amount, DateTime.UtcNow,
            TransactionCategory.Other, ConfidenceLevel.High, CategorizationSource.Dictionary, alloc);
        if (asKind is { } k && amount >= 0) t.SetKind(k);
        if (blockIdFor is not null) t.ApplyNarration(blockIdFor, null, null, null);
        return t;
    }

    [Fact]
    public void A_transaction_with_no_crop_is_shared_across_the_farms_crops_by_planted_area()
    {
        var palm = MakeBlock("A", (CropType.OilPalm, 6, 100));
        var maize = MakeBlock("B", (CropType.Maize, 2, 100));

        var pnl = GetDashboardQueryHandler.BuildCropPnl([Txn(-8000)], [palm, maize]);

        Assert.Equal(6000m, pnl.Single(p => p.CropType == "OilPalm").Cost);   // 6 of the 8 hectares
        Assert.Equal(2000m, pnl.Single(p => p.CropType == "Maize").Cost);
    }

    [Fact]
    public void A_transaction_naming_a_block_goes_to_that_blocks_crops()
    {
        var palm = MakeBlock("A", (CropType.OilPalm, 6, 100));
        var maize = MakeBlock("B", (CropType.Maize, 2, 100));

        var pnl = GetDashboardQueryHandler.BuildCropPnl([Txn(-1000, blockIdFor: maize.Id)], [palm, maize]);

        Assert.Single(pnl);
        Assert.Equal(1000m, pnl.Single(p => p.CropType == "Maize").Cost);
    }

    [Fact]
    public void A_stored_crop_split_wins_over_the_fallback()
    {
        var palm = MakeBlock("A", (CropType.OilPalm, 6, 100));
        var pnl = GetDashboardQueryHandler.BuildCropPnl([Txn(-1000, new() { ["Cocoa"] = 100 })], [palm]);
        Assert.Equal("Cocoa", pnl.Single().CropType);
    }

    [Fact]
    public void Money_in_is_revenue_and_capital_is_left_out()
    {
        var palm = MakeBlock("A", (CropType.OilPalm, 6, 100));
        var pnl = GetDashboardQueryHandler.BuildCropPnl(
            [Txn(50000), Txn(100000, asKind: TransactionKind.Capital, narration: "capital")], [palm]);

        var row = pnl.Single();
        Assert.Equal(50000m, row.Revenue);
        Assert.Equal(50000m, row.NetProfit);
    }

    [Fact]
    public void Logged_activity_costs_go_to_the_crops_of_the_block_they_were_done_on()
    {
        var mixed = MakeBlock("North", (CropType.OilPalm, 3, 60), (CropType.Cassava, 2, 40));
        var costs = new[] { new BlockCostRow(mixed.Id, 2, LabourCost: 8000m, InputCost: 2000m, MaintenanceCost: 0) };

        var pnl = GetDashboardQueryHandler.BuildCropPnl([], [mixed], costs);

        Assert.Equal(6000m, pnl.Single(p => p.CropType == "OilPalm").Cost);
        Assert.Equal(4000m, pnl.Single(p => p.CropType == "Cassava").Cost);
    }

    [Fact]
    public void With_no_activity_and_no_transactions_there_is_nothing_to_show()
    {
        var palm = MakeBlock("A", (CropType.OilPalm, 6, 100));
        Assert.Empty(GetDashboardQueryHandler.BuildCropPnl([], [palm], []));
    }

    [Fact]
    public void A_payment_matched_to_an_order_is_not_a_cost_the_activities_that_use_the_stock_are()
    {
        var palm = MakeBlock("A", (CropType.OilPalm, 6, 100));
        var payment = Txn(-50000);
        payment.MatchOrder("order-1");                       // bought stock: not yet a cost
        var costs = new[] { new BlockCostRow(palm.Id, 1, LabourCost: 4000m, InputCost: 12000m, MaintenanceCost: 0) };

        var pnl = GetDashboardQueryHandler.BuildCropPnl([payment], [palm], costs);

        Assert.Equal(16000m, pnl.Single().Cost);             // the 12,000 of stock used + 4,000 labour, not the 50,000 paid
    }

    [Fact]
    public void A_payment_flagged_for_review_still_counts_until_it_is_confirmed()
    {
        var palm = MakeBlock("A", (CropType.OilPalm, 6, 100));
        var payment = Txn(-50000);
        payment.FlagForReview("amount differs", "order-1");

        Assert.False(payment.IsStockPurchase);
        Assert.Equal(50000m, GetDashboardQueryHandler.BuildCropPnl([payment], [palm]).Single().Cost);
    }
}
