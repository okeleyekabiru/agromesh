using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

public interface IInvitationRepository
{
    Task AddAsync(Invitation invitation, CancellationToken ct = default);
    Task<Invitation?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Invitation?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<List<Invitation>> GetByCompanyAsync(string companyId, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}

public interface IPasswordResetTokenRepository
{
    Task AddAsync(PasswordResetToken token, CancellationToken ct = default);
    Task<PasswordResetToken?> GetByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task<List<PasswordResetToken>> GetByUserAsync(string userId, CancellationToken ct = default);
    Task SaveAsync(CancellationToken ct = default);
}
