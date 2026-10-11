using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

/// <summary>
/// Handler tests for the reworked Pick Stock flow — the picker is now the authenticated caller
/// (via ICurrentUserAccessor), not a client-chosen Worker, and every pick now records which
/// Block the stock is going to. Fakes mirror StockAdjustmentApprovalTests.cs's shape (no
/// mocking library, no in-memory EF).
/// </summary>
public class PickStockCommandTests
{
    private class FakeInventoryRepository : IInventoryRepository
    {
        public List<InventoryItem> Items = [];
        public List<StockMovement> Movements = [];
        public int UpdateCallCount;
        public int AddMovementCallCount;

        public Task<InventoryItem?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(i => i.Id == id));

        public Task<List<InventoryItem>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
            Task.FromResult(Items.Where(i => i.FarmId == farmId).ToList());

        public Task<List<InventoryItem>> GetLowStockAsync(string farmId, CancellationToken ct = default) =>
            Task.FromResult(new List<InventoryItem>());

        public Task<InventoryItem?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) =>
            Task.FromResult<InventoryItem?>(null);

        public Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct = default)
        {
            Items.Add(item);
            return Task.FromResult(item);
        }

        public Task UpdateAsync(InventoryItem item, CancellationToken ct = default)
        {
            UpdateCallCount++;
            return Task.CompletedTask;
        }

        public Task AddMovementAsync(StockMovement movement, CancellationToken ct = default)
        {
            AddMovementCallCount++;
            Movements.Add(movement);
            return Task.CompletedTask;
        }

        public Task UpdateMovementAsync(StockMovement movement, CancellationToken ct = default) => Task.CompletedTask;

        public Task<List<StockMovement>> GetOutstandingPicksAsync(string farmId, string? inventoryItemId = null, string? blockId = null, CancellationToken ct = default) =>
            Task.FromResult(new List<StockMovement>());

        public Task<List<StockMovement>> GetMovementsAsync(string inventoryItemId, int limit = 50, CancellationToken ct = default) =>
            Task.FromResult(Movements.Where(m => m.InventoryItemId == inventoryItemId).ToList());
    }

    private class FakeBlockRepository : IBlockRepository
    {
        public List<Block> Blocks = [];

        public Task<Block?> GetByIdAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Blocks.FirstOrDefault(s => s.Id == id));

        public Task<List<Block>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
            Task.FromResult(Blocks.Where(s => s.FarmId == farmId).ToList());

        public Task<Block> AddAsync(Block block, CancellationToken ct = default)
        {
            Blocks.Add(block);
            return Task.FromResult(block);
        }

        public Task UpdateAsync(Block block, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Blocks.Any(s => s.Id == id));
    }

    private class FakeCurrentUserAccessor : ICurrentUserAccessor
    {
        public FakeCurrentUserAccessor(string? userId = "user-1", UserRole role = UserRole.Member)
        {
            UserId = userId;
            Role = role;
        }

        public string? UserId { get; }
        public string? CompanyId => null;
        public UserRole Role { get; }
    }

    // InventoryItem.Category has no public setter (only ever populated by EF's own
    // .Include(i => i.Category) in the real repository) — PickStockCommandHandler's response DTO
    // needs item.Category.Name, so this fake-repo test attaches it via reflection, the standard
    // escape hatch for constructing a private-setter nav property outside of EF materialization.
    private static InventoryItem CreateItem(string farmId = "farm-1", decimal stock = 100)
    {
        var item = InventoryItem.Create(farmId, "Glyphosate 360", "L", "cat_chemical", stock, 30);
        var category = Domain.Entities.InventoryCategory.Create(farmId, "Chemical");
        typeof(InventoryItem).GetProperty(nameof(InventoryItem.Category))!.SetValue(item, category);
        return item;
    }

    private static Block CreateBlock(string farmId = "farm-1") =>
        Block.Create(farmId, "Block A", [new BlockCrop(CropType.OilPalm, 10, 100)], 10);

    [Fact]
    public async Task Pick_ValidRequest_DeductsStockAndAttributesCurrentUserAndBlock()
    {
        var item = CreateItem(stock: 100);
        var block = CreateBlock();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var blockRepo = new FakeBlockRepository { Blocks = [block] };
        var currentUser = new FakeCurrentUserAccessor("user-1");
        var handler = new PickStockCommandHandler(inventoryRepo, blockRepo, currentUser);

        var request = new PickStockRequest(20, block.Id, "Spraying Block A");
        var result = await handler.Handle(new PickStockCommand("farm-1", item.Id, request), CancellationToken.None);

        Assert.Equal(80m, result.Item.CurrentStock);
        Assert.Equal(1, inventoryRepo.UpdateCallCount);
        Assert.Equal(1, inventoryRepo.AddMovementCallCount);
        var movement = inventoryRepo.Movements.Single();
        Assert.Equal("user-1", movement.PerformedByUserId);
        Assert.Equal(block.Id, movement.BlockId);
        Assert.Null(movement.WorkerId);
    }

    [Fact]
    public async Task Pick_MissingBlockId_ThrowsDomainException_NoSideEffects()
    {
        var item = CreateItem(stock: 100);
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var blockRepo = new FakeBlockRepository();
        var currentUser = new FakeCurrentUserAccessor("user-1");
        var handler = new PickStockCommandHandler(inventoryRepo, blockRepo, currentUser);

        var request = new PickStockRequest(20, "", "Spraying Block A");
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PickStockCommand("farm-1", item.Id, request), CancellationToken.None));

        Assert.Equal(100m, item.CurrentStock);
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
    }

    [Fact]
    public async Task Pick_BlankPurpose_ThrowsDomainException()
    {
        var item = CreateItem(stock: 100);
        var block = CreateBlock();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var blockRepo = new FakeBlockRepository { Blocks = [block] };
        var currentUser = new FakeCurrentUserAccessor("user-1");
        var handler = new PickStockCommandHandler(inventoryRepo, blockRepo, currentUser);

        var request = new PickStockRequest(20, block.Id, "   ");
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PickStockCommand("farm-1", item.Id, request), CancellationToken.None));
    }

    [Fact]
    public async Task Pick_BlockFromDifferentFarm_ThrowsDomainException()
    {
        var item = CreateItem(farmId: "farm-1", stock: 100);
        var block = CreateBlock(farmId: "farm-2");
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var blockRepo = new FakeBlockRepository { Blocks = [block] };
        var currentUser = new FakeCurrentUserAccessor("user-1");
        var handler = new PickStockCommandHandler(inventoryRepo, blockRepo, currentUser);

        var request = new PickStockRequest(20, block.Id, "Spraying");
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PickStockCommand("farm-1", item.Id, request), CancellationToken.None));
    }

    [Fact]
    public async Task Pick_NoCurrentUser_ThrowsDomainException_NoSideEffects()
    {
        // Fails closed — a request that somehow reaches the handler without an identifiable caller
        // (should be unreachable in practice once [Authorize] gates this endpoint) never applies a
        // stock change it can't attribute to anyone.
        var item = CreateItem(stock: 100);
        var block = CreateBlock();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var blockRepo = new FakeBlockRepository { Blocks = [block] };
        var currentUser = new FakeCurrentUserAccessor(userId: null);
        var handler = new PickStockCommandHandler(inventoryRepo, blockRepo, currentUser);

        var request = new PickStockRequest(20, block.Id, "Spraying Block A");
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PickStockCommand("farm-1", item.Id, request), CancellationToken.None));

        Assert.Equal(100m, item.CurrentStock);
        Assert.Equal(0, inventoryRepo.UpdateCallCount);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
    }

    [Fact]
    public async Task Pick_InsufficientStock_ThrowsDomainException_Uncaught()
    {
        var item = CreateItem(stock: 10);
        var block = CreateBlock();
        var inventoryRepo = new FakeInventoryRepository { Items = [item] };
        var blockRepo = new FakeBlockRepository { Blocks = [block] };
        var currentUser = new FakeCurrentUserAccessor("user-1");
        var handler = new PickStockCommandHandler(inventoryRepo, blockRepo, currentUser);

        var request = new PickStockRequest(20, block.Id, "Spraying Block A");
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new PickStockCommand("farm-1", item.Id, request), CancellationToken.None));

        Assert.Equal(10m, item.CurrentStock);
        Assert.Equal(0, inventoryRepo.AddMovementCallCount);
    }
}
