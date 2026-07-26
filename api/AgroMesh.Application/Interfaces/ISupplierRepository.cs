using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Supplier>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default);
}
