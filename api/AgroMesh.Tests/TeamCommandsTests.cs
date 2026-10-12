using AgroMesh.Application.Commands.Staff;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class TeamCommandsTests
{
    private class FakeUsers : IUserRepository
    {
        public List<User> Users = [];
        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) => Task.FromResult(Users.FirstOrDefault(u => u.Email == email));
        public Task<User?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Users.FirstOrDefault(u => u.Id == id));
        public Task<List<User>> GetByCompanyAsync(string companyId, CancellationToken ct = default) => Task.FromResult(Users.Where(u => u.CompanyId == companyId).ToList());
        public Task AddAsync(User user, CancellationToken ct = default) { Users.Add(user); return Task.CompletedTask; }
        public Task SaveAsync(CancellationToken ct = default) => Task.CompletedTask;
    }

    private class Caller(string id, UserRole role, string company = "co-1") : ICurrentUserAccessor
    {
        public string? UserId => id;
        public string? CompanyId => company;
        public UserRole Role => role;
    }

    private static User Staff(UserRole role, string company = "co-1") => User.CreateStaff("Ada", $"{Guid.NewGuid():N}@farm.com", "h", company, role);

    [Fact]
    public async Task Admin_can_disable_and_re_enable_a_farm_manager()
    {
        var repo = new FakeUsers();
        var fm = Staff(UserRole.FarmManager);
        repo.Users.Add(fm);
        var handler = (bool active) => new SetUserActiveCommandHandler(repo, new Caller("admin", UserRole.Admin)).Handle(new SetUserActiveCommand(fm.Id, active), default);

        Assert.False((await handler(false)).IsActive);
        Assert.False(fm.IsActive);
        Assert.True((await handler(true)).IsActive);
    }

    [Theory]
    [InlineData(UserRole.FarmManager)]
    [InlineData(UserRole.Supervisor)]
    public async Task Only_an_admin_can_disable_anyone(UserRole caller)
    {
        var repo = new FakeUsers();
        var target = Staff(UserRole.Supervisor);
        repo.Users.Add(target);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new SetUserActiveCommandHandler(repo, new Caller("x", caller)).Handle(new SetUserActiveCommand(target.Id, false), default));
        Assert.True(target.IsActive);
    }

    [Fact]
    public async Task Someone_in_another_company_cannot_be_disabled()
    {
        var repo = new FakeUsers();
        var other = Staff(UserRole.FarmManager, "co-2");
        repo.Users.Add(other);
        await Assert.ThrowsAsync<DomainException>(() =>
            new SetUserActiveCommandHandler(repo, new Caller("admin", UserRole.Admin)).Handle(new SetUserActiveCommand(other.Id, false), default));
        Assert.True(other.IsActive);
    }

    [Fact]
    public async Task An_admin_cannot_disable_themselves_or_another_admin()
    {
        var repo = new FakeUsers();
        var self = User.Create("Boss", "boss@farm.com", "h");
        self.AssignCompany("co-1");
        repo.Users.Add(self);
        await Assert.ThrowsAsync<DomainException>(() =>
            new SetUserActiveCommandHandler(repo, new Caller(self.Id, UserRole.Admin)).Handle(new SetUserActiveCommand(self.Id, false), default));
        await Assert.ThrowsAsync<DomainException>(() =>
            new SetUserActiveCommandHandler(repo, new Caller("someone-else", UserRole.Admin)).Handle(new SetUserActiveCommand(self.Id, false), default));
    }

    [Fact]
    public async Task The_team_list_is_admin_only_and_limited_to_the_company()
    {
        var repo = new FakeUsers();
        repo.Users.Add(Staff(UserRole.FarmManager));
        repo.Users.Add(Staff(UserRole.Supervisor, "co-2"));

        var team = await new GetTeamQueryHandler(repo, new Caller("admin", UserRole.Admin)).Handle(new GetTeamQuery(), default);
        Assert.Single(team);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            new GetTeamQueryHandler(repo, new Caller("fm", UserRole.FarmManager)).Handle(new GetTeamQuery(), default));
    }
}
