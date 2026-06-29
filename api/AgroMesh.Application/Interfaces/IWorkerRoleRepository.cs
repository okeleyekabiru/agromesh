using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IWorkerRoleRepository
{
    Task<WorkerRole?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<WorkerRole>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<WorkerRole> AddAsync(WorkerRole role, CancellationToken ct = default);
    Task UpdateAsync(WorkerRole role, CancellationToken ct = default);
    Task<bool> ExistsAsync(string id, CancellationToken ct = default);
}
