using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;
using AgroMesh.Infrastructure.Services;

namespace AgroMesh.Tests;

/// <summary>Pick → use (in an activity) → return, using the real InventoryDeductionService.</summary>
public class StockUsageFlowTests
{
    private class FakeInventoryRepo : IInventoryRepository
    {
        public List<InventoryItem> Items = [];
        public List<StockMovement> Movements = [];
        public Task<InventoryItem?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
        public Task<List<InventoryItem>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(Items.Where(i => i.FarmId == farmId).ToList());
        public Task<List<InventoryItem>> GetLowStockAsync(string farmId, CancellationToken ct = default) => Task.FromResult(new List<InventoryItem>());
        public Task<InventoryItem?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) => Task.FromResult<InventoryItem?>(null);
        public Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct = default) { Items.Add(item); return Task.FromResult(item); }
        public Task UpdateAsync(InventoryItem item, CancellationToken ct = default) => Task.CompletedTask;
        public Task AddMovementAsync(StockMovement movement, CancellationToken ct = default) { Movements.Add(movement); return Task.CompletedTask; }
        public Task UpdateMovementAsync(StockMovement movement, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<StockMovement>> GetOutstandingPicksAsync(string farmId, string? inventoryItemId = null, string? blockId = null, CancellationToken ct = default) =>
            Task.FromResult(Movements
                .Where(m => m.FarmId == farmId && m.IsPick && m.Outstanding > 0
                            && (inventoryItemId == null || m.InventoryItemId == inventoryItemId)
                            && (blockId == null || m.BlockId == blockId))
                .OrderBy(m => m.Date).ToList());
        public Task<List<StockMovement>> GetMovementsAsync(string inventoryItemId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult(Movements.Where(m => m.InventoryItemId == inventoryItemId).ToList());
    }

    private class FakeBlockRepo(params Block[] blocks) : IBlockRepository
    {
        public Task<Block?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(blocks.FirstOrDefault(b => b.Id == id));
        public Task<List<Block>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(blocks.Where(b => b.FarmId == farmId).ToList());
        public Task<Block> AddAsync(Block block, CancellationToken ct = default) => Task.FromResult(block);
        public Task UpdateAsync(Block block, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(blocks.Any(b => b.Id == id));
    }

    private class FakeFarmRepo(Farm farm) : IFarmRepository
    {
        public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(farm.Id == id ? farm : null);
        public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
        public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<Farm> AddAsync(Farm f, CancellationToken ct = default) => Task.FromResult(f);
        public Task UpdateAsync(Farm f, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(farm.Id == id);
    }

    private class FakeUser : ICurrentUserAccessor
    {
        public string? UserId => "user-1";
        public string? CompanyId => "co-1";
        public UserRole Role => UserRole.Admin;
    }

    private readonly Farm _farm = Farm.Create("Farm", "co-1", "owner");
    private readonly FakeInventoryRepo _repo = new();
    private readonly Block _blockA;
    private readonly Block _blockB;
    private readonly InventoryItem _npk;

    public StockUsageFlowTests()
    {
        _blockA = Block.Create(_farm.Id, "Block A", [new BlockCrop(CropType.OilPalm, 10, 100)], 10);
        _blockB = Block.Create(_farm.Id, "Block B", [new BlockCrop(CropType.OilPalm, 10, 100)], 10);
        _npk = InventoryItem.Create(_farm.Id, "NPK Fertilizer", "kg", "cat", 200, 20);
        typeof(InventoryItem).GetProperty(nameof(InventoryItem.Category))!
            .SetValue(_npk, InventoryCategory.Create(_farm.Id, "Fertilizer"));
        _repo.Items.Add(_npk);
    }

    private InventoryDeductionService Service => new(_repo);

    private async Task Pick(decimal kg, Block block)
    {
        // Same effect as PickStockCommand: deduct from stock and record an Out movement with a picker.
        _npk.DeductStock(kg);
        await _repo.AddMovementAsync(StockMovement.CreateOut(_farm.Id, _npk.Id, kg, notes: "pick", performedByUserId: "user-1", blockId: block.Id));
    }

    private async Task<List<string>> Use(decimal kg, Block block)
    {
        var inputs = new List<InputUsedRequest> { new(_npk.Id, (double)kg) };
        var resolved = await Service.ResolveAsync(_farm.Id, block.Id, inputs);
        return await Service.ApplyAsync(_farm.Id, block.Id, "act-1", resolved);
    }

    private ReturnStockCommandHandler ReturnHandler => new(_repo, new FakeBlockRepo(_blockA, _blockB), new FakeFarmRepo(_farm), new FakeUser());

    [Fact]
    public async Task Pick50_Use38_StockDoesNotDropTwice_ThenReturn12_RestoresLeftover()
    {
        await Pick(50, _blockA);                       // store: 200 -> 150
        Assert.Equal(150m, _npk.CurrentStock);

        await Use(38, _blockA);                        // uses picked stock — store unchanged
        Assert.Equal(150m, _npk.CurrentStock);
        var pick = _repo.Movements.Single(m => m.IsPick);
        Assert.Equal(38m, pick.UsedQuantity);
        Assert.Equal(12m, pick.Outstanding);

        await ReturnHandler.Handle(new ReturnStockCommand(_farm.Id, _npk.Id, new ReturnStockRequest(12, _blockA.Id, "leftover")), CancellationToken.None);

        Assert.Equal(162m, _npk.CurrentStock);         // 200 - 38 actually used
        Assert.Equal(0m, pick.Outstanding);
        Assert.Contains(_repo.Movements, m => m.Type == StockMovementType.Return && m.Quantity == 12);
    }

    [Fact]
    public async Task UsingMoreThanPicked_TakesTheShortfallFromTheStore()
    {
        await Pick(10, _blockA);                       // store 190, 10 out
        await Use(25, _blockA);                        // 10 from the pick + 15 from the store

        Assert.Equal(175m, _npk.CurrentStock);
        Assert.Equal(0m, _repo.Movements.Single(m => m.IsPick).Outstanding);
        var storeUse = _repo.Movements.Single(m => m.Type == StockMovementType.Out && !m.IsPick);
        Assert.Equal(15m, storeUse.Quantity);
        Assert.Equal("act-1", storeUse.ActivityLogId);
    }

    [Fact]
    public async Task UsingWithoutAnyPick_DeductsFromTheStore()
    {
        await Use(30, _blockA);
        Assert.Equal(170m, _npk.CurrentStock);
    }

    [Fact]
    public async Task PickedForAnotherBlock_IsNotConsumed()
    {
        await Pick(50, _blockB);                       // store 150, 50 out for B
        await Use(20, _blockA);                        // nothing picked for A -> from the store

        Assert.Equal(130m, _npk.CurrentStock);
        Assert.Equal(50m, _repo.Movements.Single(m => m.IsPick).Outstanding);
    }

    [Fact]
    public async Task NotEnoughInTotal_IsRejected_AndNothingChanges()
    {
        await Pick(190, _blockA);                      // store 10, 190 out for A
        var inputs = new List<InputUsedRequest> { new(_npk.Id, 500) };

        await Assert.ThrowsAsync<DomainException>(() => Service.ResolveAsync(_farm.Id, _blockA.Id, inputs));
        Assert.Equal(10m, _npk.CurrentStock);
        Assert.Equal(190m, _repo.Movements.Single(m => m.IsPick).Outstanding);
    }

    [Fact]
    public async Task DuplicateInputs_AreCheckedTogether()
    {
        var inputs = new List<InputUsedRequest> { new(_npk.Id, 150), new(_npk.Id, 150) };
        await Assert.ThrowsAsync<DomainException>(() => Service.ResolveAsync(_farm.Id, _blockA.Id, inputs));
    }

    [Fact]
    public async Task ItemOfAnotherFarm_IsRejected()
    {
        var foreign = InventoryItem.Create("other-farm", "Foreign", "kg", "cat", 100, 1);
        _repo.Items.Add(foreign);
        await Assert.ThrowsAsync<DomainException>(() =>
            Service.ResolveAsync(_farm.Id, _blockA.Id, [new InputUsedRequest(foreign.Id, 1)]));
    }

    [Fact]
    public async Task ZeroQuantity_IsRejected()
    {
        await Assert.ThrowsAsync<DomainException>(() =>
            Service.ResolveAsync(_farm.Id, _blockA.Id, [new InputUsedRequest(_npk.Id, 0)]));
    }

    [Fact]
    public async Task Return_MoreThanOutstanding_IsRejected_AndStockUnchanged()
    {
        await Pick(50, _blockA);
        await Use(38, _blockA);

        await Assert.ThrowsAsync<DomainException>(() =>
            ReturnHandler.Handle(new ReturnStockCommand(_farm.Id, _npk.Id, new ReturnStockRequest(13, _blockA.Id, null)), CancellationToken.None));
        Assert.Equal(150m, _npk.CurrentStock);
    }

    [Fact]
    public async Task Return_ForABlockWithNothingOut_IsRejected()
    {
        await Pick(50, _blockA);
        await Assert.ThrowsAsync<DomainException>(() =>
            ReturnHandler.Handle(new ReturnStockCommand(_farm.Id, _npk.Id, new ReturnStockRequest(5, _blockB.Id, null)), CancellationToken.None));
    }

    [Fact]
    public async Task Return_SettlesOldestPicksFirst_AcrossSeveralPicks()
    {
        await Pick(10, _blockA);
        await Task.Delay(5);
        await Pick(20, _blockA);

        await ReturnHandler.Handle(new ReturnStockCommand(_farm.Id, _npk.Id, new ReturnStockRequest(15, _blockA.Id, null)), CancellationToken.None);

        var picks = _repo.Movements.Where(m => m.IsPick).OrderBy(m => m.Date).ToList();
        Assert.Equal(0m, picks[0].Outstanding);   // fully returned
        Assert.Equal(15m, picks[1].Outstanding);  // 5 of it returned
        Assert.Equal(185m, _npk.CurrentStock);    // 200 - 30 picked + 15 returned
    }

    [Fact]
    public async Task Return_OfZeroOrNegative_IsRejected()
    {
        await Pick(50, _blockA);
        await Assert.ThrowsAsync<DomainException>(() =>
            ReturnHandler.Handle(new ReturnStockCommand(_farm.Id, _npk.Id, new ReturnStockRequest(0, _blockA.Id, null)), CancellationToken.None));
    }

    [Fact]
    public void ReturnMovement_IsNotAPick()
    {
        var ret = StockMovement.CreateReturn(_farm.Id, _npk.Id, 5, _blockA.Id, "user-1");
        Assert.False(ret.IsPick);
        Assert.Equal(0m, ret.Outstanding);
    }
}
