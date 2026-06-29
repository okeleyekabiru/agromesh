using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IWorkerRepository
{
    Task<Worker?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Worker?> GetByIdWithRoleAsync(string id, CancellationToken ct = default);
    Task<List<Worker>> GetByFarmIdAsync(string farmId, bool activeOnly = true, CancellationToken ct = default);
    Task<Worker> AddAsync(Worker worker, CancellationToken ct = default);
    Task UpdateAsync(Worker worker, CancellationToken ct = default);
    Task<int> GetCountByRoleAsync(string workerRoleId, CancellationToken ct = default);
}
