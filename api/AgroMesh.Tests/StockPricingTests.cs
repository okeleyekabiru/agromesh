using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class StockPricingTests
{
    private static InventoryItem Npk(decimal stock = 0)
    {
        var item = InventoryItem.Create("farm-1", "NPK", "kg", "cat", stock, 10);
        typeof(InventoryItem).GetProperty(nameof(InventoryItem.Category))!
            .SetValue(item, InventoryCategory.Create("farm-1", "Fertilizer"));
        return item;
    }

    [Fact]
    public void First_purchase_sets_the_cost_per_unit()
    {
        var item = Npk();
        item.SetPack("bag", 50);
        item.ReceiveStock(100, 28000m / 50);   // 2 bags at 28,000
        Assert.Equal(100, item.CurrentStock);
        Assert.Equal(560m, item.UnitCost);
    }

    [Fact]
    public void A_later_purchase_at_a_new_price_blends_into_a_weighted_average()
    {
        var item = Npk();
        item.ReceiveStock(100, 500m);
        item.ReceiveStock(100, 700m);
        Assert.Equal(600m, item.UnitCost);
    }

    [Fact]
    public void Stock_that_had_no_price_takes_the_new_price_instead_of_diluting_it()
    {
        var item = Npk(stock: 80);
        item.ReceiveStock(20, 600m);
        Assert.Equal(600m, item.UnitCost);
    }

    [Fact]
    public void Receiving_without_a_price_keeps_the_existing_cost()
    {
        var item = Npk();
        item.ReceiveStock(10, 500m);
        item.ReceiveStock(10, null);
        Assert.Equal(500m, item.UnitCost);
        Assert.Equal(20, item.CurrentStock);
    }

    [Fact]
    public void Pack_size_must_be_positive()
    {
        Assert.Throws<DomainException>(() => Npk().SetPack("bag", 0));
    }

    private class FakeInventoryRepo(params InventoryItem[] items) : IInventoryRepository
    {
        public Task<InventoryItem?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(items.FirstOrDefault(i => i.Id == id));
        public Task<List<InventoryItem>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(items.ToList());
        public Task<List<InventoryItem>> GetLowStockAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InventoryItem?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(InventoryItem item, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddMovementAsync(StockMovement movement, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateMovementAsync(StockMovement movement, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<StockMovement>> GetOutstandingPicksAsync(string farmId, string? inventoryItemId = null, string? blockId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<StockMovement>> GetMovementsAsync(string inventoryItemId, int limit = 50, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private class FakeActivityRepo(params ActivityLog[] unpriced) : IActivityRepository
    {
        public List<ActivityLog> Updated = [];
        public Task<ActivityLog?> GetByIdAsync(string id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(List<ActivityLog> Items, int Total)> GetByFarmIdAsync(string farmId, int page, int pageSize, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ActivityLog>> GetRecentByBlockAsync(string blockId, ActivityType activityType, int limit = 5, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ActivityLog>> GetRecentByFarmIdAsync(string farmId, int limit = 10, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<ActivityLog> AddAsync(ActivityLog activity, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<ActivityLog>> GetBetweenAsync(string farmId, DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(new List<ActivityLog>());
        public Task UpdateAsync(ActivityLog activity, CancellationToken ct = default) { Updated.Add(activity); return Task.CompletedTask; }
        public Task<List<ActivityLog>> GetUnpricedUsingItemAsync(string farmId, string inventoryItemId, CancellationToken ct = default) => Task.FromResult(unpriced.ToList());
        public Task<int> GetCountThisMonthAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<decimal> GetLabourCostMtdAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<MonthAmount>> GetMonthlyCostsAsync(string farmId, DateTime from, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<BlockCostRow>> GetBlockCostsMtdAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static ActivityLog Activity(string itemId, double kg) =>
        ActivityLog.Create("farm-1", "block-1", ActivityType.FertilizerApplication, DateTime.UtcNow, [],
            [new InputUsed("NPK", kg, "kg", itemId)], "sup", null, false, null, null, null, null);

    [Fact]
    public async Task Pricing_an_item_backfills_activities_that_had_no_input_cost()
    {
        var npk = Npk(100);
        var past = Activity(npk.Id, 38);
        var acts = new FakeActivityRepo(past);

        var res = await new SetItemPricingCommandHandler(new FakeInventoryRepo(npk), acts)
            .Handle(new SetItemPricingCommand("farm-1", npk.Id, new SetItemPricingRequest(50, "bag", 28000, true)), default);

        Assert.Equal(560m, res.Item.UnitCost);
        Assert.Equal(1, res.ActivitiesUpdated);
        Assert.Equal(38 * 560m, past.InputCost);
    }

    [Fact]
    public async Task Backfill_can_be_switched_off()
    {
        var npk = Npk(100);
        var past = Activity(npk.Id, 10);

        var res = await new SetItemPricingCommandHandler(new FakeInventoryRepo(npk), new FakeActivityRepo(past))
            .Handle(new SetItemPricingCommand("farm-1", npk.Id, new SetItemPricingRequest(50, "bag", 28000, false)), default);

        Assert.Equal(0, res.ActivitiesUpdated);
        Assert.Equal(0m, past.InputCost);
    }

    [Fact]
    public async Task Another_farms_item_is_not_found()
    {
        var npk = Npk(100);
        await Assert.ThrowsAsync<DomainException>(() =>
            new SetItemPricingCommandHandler(new FakeInventoryRepo(npk), new FakeActivityRepo())
                .Handle(new SetItemPricingCommand("other-farm", npk.Id, new SetItemPricingRequest(50, "bag", 1, false)), default));
    }
}
