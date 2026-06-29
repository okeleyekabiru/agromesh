using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IFarmRepository
{
    Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Farm?> GetByIdWithSectionsAsync(string id, CancellationToken ct = default);
    Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default);
    Task<Farm> AddAsync(Farm farm, CancellationToken ct = default);
    Task UpdateAsync(Farm farm, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
}
