using AgroMesh.Application.Commands.Staff;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class AddStaffUserCommandTests
{
    private class FakeUserRepository : IUserRepository
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

    private class FakeHasher : IPasswordHasher
    {
        public string Hash(string password) => "hash:" + password;
        public bool Verify(string password, string hash) => hash == "hash:" + password;
    }

    private class FakeCurrentUser(UserRole role, string? companyId = "co-1") : ICurrentUserAccessor
    {
        public string? UserId => "caller";
        public string? CompanyId => companyId;
        public UserRole Role => role;
    }

    private static (AddStaffUserCommandHandler handler, FakeUserRepository repo) Create(UserRole caller, string? companyId = "co-1")
    {
        var repo = new FakeUserRepository();
        return (new AddStaffUserCommandHandler(repo, new FakeHasher(), new FakeCurrentUser(caller, companyId)), repo);
    }

    private static AddStaffUserCommand Cmd(string role, string email = "new@farm.com") =>
        new(new AddStaffUserRequest("New Person", email, "password123", role));

    [Theory]
    [InlineData(UserRole.Admin, "FarmManager")]
    [InlineData(UserRole.Admin, "Supervisor")]
    [InlineData(UserRole.FarmManager, "Supervisor")]
    public async Task Allowed_CreatesUserInCallersCompany(UserRole caller, string target)
    {
        var (handler, repo) = Create(caller);
        var dto = await handler.Handle(Cmd(target), CancellationToken.None);

        var user = Assert.Single(repo.Users);
        Assert.Equal("co-1", user.CompanyId);
        Assert.Equal(target, dto.Role);
        Assert.Equal("hash:password123", user.PasswordHash);
    }

    [Theory]
    [InlineData(UserRole.FarmManager, "FarmManager")]
    [InlineData(UserRole.Supervisor, "Supervisor")]
    [InlineData(UserRole.Supervisor, "FarmManager")]
    [InlineData(UserRole.Member, "Supervisor")]
    public async Task NotAllowed_ThrowsForbidden_AndCreatesNothing(UserRole caller, string target)
    {
        var (handler, repo) = Create(caller);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(Cmd(target), CancellationToken.None));
        Assert.Empty(repo.Users);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Member")]
    [InlineData("nonsense")]
    public async Task UnassignableRole_ThrowsDomainException(string role)
    {
        var (handler, repo) = Create(UserRole.Admin);
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(Cmd(role), CancellationToken.None));
        Assert.Empty(repo.Users);
    }

    [Fact]
    public async Task DuplicateEmail_Throws()
    {
        var (handler, repo) = Create(UserRole.Admin);
        await handler.Handle(Cmd("Supervisor", "Dup@Farm.com"), CancellationToken.None);
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(Cmd("Supervisor", "dup@farm.com"), CancellationToken.None));
        Assert.Single(repo.Users);
    }

    [Fact]
    public async Task ShortPassword_Throws()
    {
        var (handler, _) = Create(UserRole.Admin);
        var cmd = new AddStaffUserCommand(new AddStaffUserRequest("N", "a@b.com", "short", "Supervisor"));
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(cmd, CancellationToken.None));
    }

    [Fact]
    public async Task CallerWithoutCompany_ThrowsForbidden()
    {
        var (handler, _) = Create(UserRole.Admin, companyId: null);
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(Cmd("Supervisor"), CancellationToken.None));
    }
}
