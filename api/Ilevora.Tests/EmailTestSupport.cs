using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;

namespace Ilevora.Tests;

internal class FakeEmailSender : IEmailSender
{
    public List<EmailMessage> Sent = [];
    public bool Throw;

    public Task SendAsync(EmailMessage message, CancellationToken ct = default)
    {
        if (Throw) throw new InvalidOperationException("resend down");
        Sent.Add(message);
        return Task.CompletedTask;
    }
}

internal class FakeAppUrls : IAppUrls
{
    public string AcceptInvite(string token) => "https://app.test/accept-invite?token=" + token;
    public string ResetPassword(string token) => "https://app.test/reset-password?token=" + token;
    public string Login => "https://app.test/";
}

internal class TestClock(DateTimeOffset start) : TimeProvider
{
    private DateTimeOffset _now = start;
    public override DateTimeOffset GetUtcNow() => _now;
    public void Advance(TimeSpan by) => _now += by;
}

internal class InMemoryUsers : IUserRepository
{
    public List<User> Users = [];
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == email.Trim().ToLowerInvariant()));
    public Task<User?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
    public Task<List<User>> GetByCompanyAsync(string companyId, CancellationToken ct = default) =>
        Task.FromResult(Users.Where(u => u.CompanyId == companyId).ToList());
    public Task AddAsync(User user, CancellationToken ct = default) { Users.Add(user); return Task.CompletedTask; }
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal class InMemoryInvitations : IInvitationRepository
{
    public List<Invitation> Items = [];
    public Task AddAsync(Invitation i, CancellationToken ct = default) { Items.Add(i); return Task.CompletedTask; }
    public Task<Invitation?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.Id == id));
    public Task<Invitation?> GetByTokenHashAsync(string hash, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.TokenHash == hash));
    public Task<List<Invitation>> GetByCompanyAsync(string companyId, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(i => i.CompanyId == companyId).ToList());
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal class InMemoryResetTokens : IPasswordResetTokenRepository
{
    public List<PasswordResetToken> Items = [];
    public Task AddAsync(PasswordResetToken t, CancellationToken ct = default) { Items.Add(t); return Task.CompletedTask; }
    public Task<PasswordResetToken?> GetByTokenHashAsync(string hash, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(i => i.TokenHash == hash));
    public Task<List<PasswordResetToken>> GetByUserAsync(string userId, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(i => i.UserId == userId).ToList());
    public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
}

internal class InMemoryCompanies : ICompanyRepository
{
    public List<Company> Items = [];
    public Task<Company?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Items.FirstOrDefault(c => c.Id == id));
    public Task<Company> AddAsync(Company c, CancellationToken ct = default) { Items.Add(c); return Task.FromResult(c); }
}

internal class StubFarms : IFarmRepository
{
    public List<Farm> Farms = [];
    public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Farms.FirstOrDefault(f => f.Id == id));
    public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
    public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(Farms);
    public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(Farms);
    public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) =>
        Task.FromResult(Farms.Where(f => f.CompanyId == companyId).ToList());
    public Task<Farm> AddAsync(Farm farm, CancellationToken ct = default) { Farms.Add(farm); return Task.FromResult(farm); }
    public Task UpdateAsync(Farm farm, CancellationToken ct = default) => Task.CompletedTask;
    public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(Farms.Any(f => f.Id == id));
}

internal class FakeHasher2 : IPasswordHasher
{
    public string Hash(string password) => "hash:" + password;
    public bool Verify(string password, string hash) => hash == "hash:" + password;
}

internal class FakeJwt : IJwtService
{
    public string GenerateToken(User user) => "jwt-for-" + user.Id;
}

internal class FakeCaller(UserRole role, string? companyId = "co-1", string? userId = "caller") : ICurrentUserAccessor
{
    public string? UserId => userId;
    public string? CompanyId => companyId;
    public UserRole Role => role;
}
