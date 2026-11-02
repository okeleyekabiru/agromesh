using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly IlevoraDbContext _db;

    public UserRepository(IlevoraDbContext db) => _db = db;

    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        _db.Users.FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public Task<User?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _db.Users.FindAsync([id], ct).AsTask();

    public Task<List<User>> GetByCompanyAsync(string companyId, CancellationToken ct = default) =>
        _db.Users.Where(u => u.CompanyId == companyId).OrderBy(u => u.CreatedAt).ToListAsync(ct);

    public async Task AddAsync(User user, CancellationToken ct = default) =>
        await _db.Users.AddAsync(user, ct);

    public Task SaveAsync(CancellationToken ct = default) =>
        _db.SaveChangesAsync(ct);
}
