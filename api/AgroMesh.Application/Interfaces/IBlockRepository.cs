using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IBlockRepository
{
    Task<Block?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Block>> GetBySectionIdAsync(string sectionId, CancellationToken ct = default);
    Task<List<Block>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<Block> AddAsync(Block block, CancellationToken ct = default);
    Task UpdateAsync(Block block, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
}
