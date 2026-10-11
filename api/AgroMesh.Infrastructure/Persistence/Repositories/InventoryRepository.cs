using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AgroMeshDbContext _db;

    public InventoryRepository(AgroMeshDbContext db) => _db = db;

    public async Task<InventoryItem?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.InventoryItems.Include(i => i.Category).FirstOrDefaultAsync(i => i.Id == id, ct);

    public async Task<List<InventoryItem>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.InventoryItems
            .Include(i => i.Category)
            .Where(i => i.FarmId == farmId)
            .OrderBy(i => i.Category.Name)
            .ThenBy(i => i.Name)
            .ToListAsync(ct);

    public async Task<List<InventoryItem>> GetLowStockAsync(string farmId, CancellationToken ct = default) =>
        await _db.InventoryItems
            .Where(i => i.FarmId == farmId && i.CurrentStock <= i.ReorderThreshold)
            .ToListAsync(ct);

    public async Task<InventoryItem?> FindByNameAsync(string farmId, string name, CancellationToken ct = default) =>
        await _db.InventoryItems
            .Include(i => i.Category)
            .FirstOrDefaultAsync(i => i.FarmId == farmId && i.Name.ToLower() == name.ToLower(), ct);

    public async Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct = default)
    {
        _db.InventoryItems.Add(item);
        await _db.SaveChangesAsync(ct);
        return item;
    }

    public async Task UpdateAsync(InventoryItem item, CancellationToken ct = default)
    {
        _db.InventoryItems.Update(item);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AddMovementAsync(StockMovement movement, CancellationToken ct = default)
    {
        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateMovementAsync(StockMovement movement, CancellationToken ct = default)
    {
        _db.StockMovements.Update(movement);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<StockMovement>> GetOutstandingPicksAsync(
        string farmId, string? inventoryItemId = null, string? blockId = null, CancellationToken ct = default)
    {
        var query = _db.StockMovements.Where(m =>
            m.FarmId == farmId &&
            m.Type == StockMovementType.Out &&
            m.PerformedByUserId != null &&
            m.Quantity - m.UsedQuantity - m.ReturnedQuantity > 0);
        if (inventoryItemId != null) query = query.Where(m => m.InventoryItemId == inventoryItemId);
        if (blockId != null) query = query.Where(m => m.BlockId == blockId);
        return await query.OrderBy(m => m.Date).ToListAsync(ct);
    }

    public async Task<List<StockMovement>> GetMovementsAsync(string inventoryItemId, int limit = 50, CancellationToken ct = default) =>
        await _db.StockMovements
            .Where(m => m.InventoryItemId == inventoryItemId)
            .OrderByDescending(m => m.Date)
            .Take(limit)
            .ToListAsync(ct);
}
