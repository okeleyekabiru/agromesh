using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

/// <summary>Custom (per-farm) crops. Built-in crops live in <c>BuiltInCrop</c>, not here.</summary>
public interface ICropRepository
{
    Task<Crop?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Crop>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<Crop> AddAsync(Crop crop, CancellationToken ct = default);
}
