using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Company> AddAsync(Company company, CancellationToken ct = default);
}
