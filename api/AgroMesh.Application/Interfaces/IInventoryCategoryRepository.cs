using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IInventoryCategoryRepository
{
    Task<InventoryCategory?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<InventoryCategory>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<InventoryCategory?> FindByNameAsync(string farmId, string name, CancellationToken ct = default);
    Task<InventoryCategory> AddAsync(InventoryCategory category, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<InventoryCategory> categories, CancellationToken ct = default);
}
