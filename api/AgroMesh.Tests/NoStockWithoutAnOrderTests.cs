using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class NoStockWithoutAnOrderTests
{
    private class Items : IInventoryRepository
    {
        public List<InventoryItem> All = [];
        public int Added;
        public Task<InventoryItem?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(i => i.Id == id));
        public Task<InventoryItem?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) =>
            Task.FromResult(All.FirstOrDefault(i => i.FarmId == farmId && string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase)));
        public Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct = default) { All.Add(item); Added++; return Task.FromResult(item); }
        public Task UpdateAsync(InventoryItem item, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddMovementAsync(StockMovement movement, CancellationToken ct = default) => throw new InvalidOperationException("no stock may move");
        public Task<List<InventoryItem>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<InventoryItem>> GetLowStockAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateMovementAsync(StockMovement movement, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<StockMovement>> GetOutstandingPicksAsync(string farmId, string? inventoryItemId = null, string? blockId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<StockMovement>> GetMovementsAsync(string inventoryItemId, int limit = 50, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private class Cats : IInventoryCategoryRepository
    {
        public Task<InventoryCategory?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult<InventoryCategory?>(InventoryCategory.Create("farm-1", "Fertilizer"));
        public Task<List<InventoryCategory>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InventoryCategory?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<InventoryCategory> AddAsync(InventoryCategory category, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AddRangeAsync(IEnumerable<InventoryCategory> categories, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private static CreateInventoryItemRequest Request(decimal quantity, string? existing = null) =>
        new("NPK", "kg", "cat1", quantity, 10, null, existing, 50, "bag", 28000);

    [Fact]
    public async Task Adding_an_item_with_a_quantity_is_refused_and_nothing_is_stored()
    {
        var repo = new Items();
        var handler = new CreateInventoryItemCommandHandler(repo, new Cats());

        var ex = await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateInventoryItemCommand("farm-1", Request(100)), default));

        Assert.Contains("purchase order", ex.Message);
        Assert.Equal(0, repo.Added);
    }

    [Fact]
    public async Task Restocking_an_existing_item_directly_is_refused()
    {
        var item = InventoryItem.Create("farm-1", "NPK", "kg", "cat1", 0, 10);
        var handler = new CreateInventoryItemCommandHandler(new Items { All = [item] }, new Cats());

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new CreateInventoryItemCommand("farm-1", Request(50, item.Id)), default));

        Assert.Equal(0m, item.CurrentStock);
    }

    [Fact]
    public async Task An_item_can_be_added_to_the_catalogue_with_no_stock()
    {
        var repo = new Items();
        var handler = new CreateInventoryItemCommandHandler(repo, new Cats());

        var dto = await handler.Handle(new CreateInventoryItemCommand("farm-1", Request(0)), default);

        Assert.Equal(1, repo.Added);
        Assert.Equal(0m, dto.CurrentStock);
        Assert.Equal(0m, dto.UnitCost);                    // the cost arrives with the first received order
    }
}
