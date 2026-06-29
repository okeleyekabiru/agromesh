using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface ISectionRepository
{
    Task<Section?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Section?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default);
    Task<List<Section>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<Section> AddAsync(Section section, CancellationToken ct = default);
    Task UpdateAsync(Section section, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
}
