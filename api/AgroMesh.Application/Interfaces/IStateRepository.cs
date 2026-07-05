using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IStateRepository
{
    Task<List<State>> GetAllAsync(CancellationToken ct = default);
    Task<State?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<bool> AnyAsync(CancellationToken ct = default);
    Task AddRangeAsync(List<State> states, CancellationToken ct = default);
}
