using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

public interface ISupplierRepository
{
    Task<Supplier?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Supplier>> GetByFarmIdAsync(string farmId, CancellationToken ct = default);
    Task<Supplier> AddAsync(Supplier supplier, CancellationToken ct = default);
}
