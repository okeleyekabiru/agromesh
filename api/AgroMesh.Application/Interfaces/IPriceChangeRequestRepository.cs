using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface IPriceChangeRequestRepository
{
    Task<PriceChangeRequest?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<PriceChangeRequest>> GetByFarmIdAsync(string farmId, PriceChangeStatus? status = null, CancellationToken ct = default);
    Task<PriceChangeRequest> AddAsync(PriceChangeRequest request, CancellationToken ct = default);
    Task UpdateAsync(PriceChangeRequest request, CancellationToken ct = default);
}
