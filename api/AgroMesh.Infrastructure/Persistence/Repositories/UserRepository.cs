using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class UserRepository : IUserRepository
{
    private readonly AgroMeshDbContext _db;

    public UserRepository(AgroMeshDbContext db) => _db = db;

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
