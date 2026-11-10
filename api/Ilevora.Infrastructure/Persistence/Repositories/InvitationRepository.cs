using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class InvitationRepository : IInvitationRepository
{
    private readonly IlevoraDbContext _db;

    public InvitationRepository(IlevoraDbContext db) => _db = db;

    public async Task AddAsync(Invitation invitation, CancellationToken ct = default) => await _db.Invitations.AddAsync(invitation, ct);

    public Task<Invitation?> GetByIdAsync(string id, CancellationToken ct = default) =>
        _db.Invitations.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.Invitations.FirstOrDefaultAsync(i => i.TokenHash == tokenHash, ct);

    public Task<List<Invitation>> GetByCompanyAsync(string companyId, CancellationToken ct = default) =>
        _db.Invitations.Where(i => i.CompanyId == companyId).OrderByDescending(i => i.CreatedAt).ToListAsync(ct);

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}

public class PasswordResetTokenRepository : IPasswordResetTokenRepository
{
    private readonly IlevoraDbContext _db;

    public PasswordResetTokenRepository(IlevoraDbContext db) => _db = db;

    public async Task AddAsync(PasswordResetToken token, CancellationToken ct = default) => await _db.PasswordResetTokens.AddAsync(token, ct);

    public Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default) =>
        _db.PasswordResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

    public Task<List<PasswordResetToken>> GetByUserAsync(string userId, CancellationToken ct = default) =>
        _db.PasswordResetTokens.Where(t => t.UserId == userId).ToListAsync(ct);

    public Task SaveAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
