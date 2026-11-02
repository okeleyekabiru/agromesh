using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

public interface IChemicalProductRepository
{
    Task<ChemicalProduct?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<ChemicalProduct>> GetAllActiveAsync(CancellationToken ct = default);
    Task<ChemicalProduct> AddAsync(ChemicalProduct product, CancellationToken ct = default);
    Task UpdateAsync(ChemicalProduct product, CancellationToken ct = default);
}
