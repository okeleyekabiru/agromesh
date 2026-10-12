using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<User?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<User>> GetByCompanyAsync(string companyId, CancellationToken ct = default);
    Task AddAsync(User user, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
