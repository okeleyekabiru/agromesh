using AgroMesh.Application.Queries.Dashboard;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Tests;

public class DashboardCropPnlTests
{
    private static Transaction Txn(decimal amount, Dictionary<string, double>? alloc) =>
        Transaction.Create("f1", Guid.NewGuid().ToString(), "n", amount, DateTime.UtcNow,
            TransactionCategory.Other, ConfidenceLevel.High, CategorizationSource.Dictionary, alloc);

    [Fact]
    public void Splits_costs_and_revenue_across_crops_by_percentage()
    {
        var pnl = GetDashboardQueryHandler.BuildCropPnl(
        [
            Txn(-1000, new() { ["Oil Palm"] = 70, ["Maize"] = 30 }),
            Txn(500, new() { ["Maize"] = 100 }),
        ]);

        var palm = pnl.Single(p => p.CropType == "Oil Palm");
        var maize = pnl.Single(p => p.CropType == "Maize");
        Assert.Equal(700m, palm.Cost);
        Assert.Equal(-700m, palm.NetProfit);
        Assert.Equal(300m, maize.Cost);
        Assert.Equal(500m, maize.Revenue);
        Assert.Equal(200m, maize.NetProfit);
    }

    [Fact]
    public void Ignores_transactions_not_yet_allocated_to_a_crop()
    {
        Assert.Empty(GetDashboardQueryHandler.BuildCropPnl([Txn(-1000, null), Txn(-5, new())]));
    }
}
