using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<InventoryItem>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<List<InventoryItem>> GetLowStockAsync(string farmId, CancellationToken ct = default);
    Task<InventoryItem?> FindByNameAsync(string farmId, string name, CancellationToken ct = default);
    Task<InventoryItem> AddAsync(InventoryItem item, CancellationToken ct = default);
    Task UpdateAsync(InventoryItem item, CancellationToken ct = default);
    Task AddMovementAsync(StockMovement movement, CancellationToken ct = default);
    Task UpdateMovementAsync(StockMovement movement, CancellationToken ct = default);
    /// <summary>
    /// Manual picks with stock still out on the farm, oldest first. Optionally narrowed to one item
    /// and/or one block.
    /// </summary>
    Task<List<StockMovement>> GetOutstandingPicksAsync(string farmId, string? inventoryItemId = null, string? blockId = null, CancellationToken ct = default);
    Task<List<StockMovement>> GetMovementsAsync(string inventoryItemId, int limit = 50, CancellationToken ct = default);
}
