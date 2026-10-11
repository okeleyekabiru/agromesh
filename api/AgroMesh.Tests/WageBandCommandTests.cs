using AgroMesh.Application.Commands.WorkerRole;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class WageBandCommandTests
{
    private class FakeRoleRepo : IWorkerRoleRepository
    {
        public List<WorkerRole> Roles = [];
        public int UpdateCalls;
        public Task<WorkerRole?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Roles.FirstOrDefault(r => r.Id == id));
        public Task<List<WorkerRole>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(Roles.Where(r => r.FarmId == farmId).ToList());
        public Task<WorkerRole> AddAsync(WorkerRole role, CancellationToken ct = default) { Roles.Add(role); return Task.FromResult(role); }
        public Task UpdateAsync(WorkerRole role, CancellationToken ct = default) { UpdateCalls++; return Task.CompletedTask; }
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(Roles.Any(r => r.Id == id));
    }

    private class FakeFarmRepo(Farm farm) : IFarmRepository
    {
        public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(farm.Id == id ? farm : null);
        public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
        public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<Farm> AddAsync(Farm f, CancellationToken ct = default) => Task.FromResult(f);
        public Task UpdateAsync(Farm f, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(farm.Id == id);
    }

    private class FakeUser(UserRole role, string? companyId = "co-1") : ICurrentUserAccessor
    {
        public string? UserId => "u1";
        public string? CompanyId => companyId;
        public UserRole Role => role;
    }

    private static CreateWorkerRoleRequest Req(decimal rate = 5000, string title = "Harvester") =>
        new(title, WorkerType.Employee, rate, PayFrequency.Daily);

    private static (FakeRoleRepo repo, Farm farm) Setup() => (new FakeRoleRepo(), Farm.Create("Farm", "co-1", "owner"));

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.FarmManager)]
    public async Task Create_AllowedRoles_AddsBand(UserRole caller)
    {
        var (repo, farm) = Setup();
        var handler = new CreateWorkerRoleCommandHandler(repo, new FakeFarmRepo(farm), new FakeUser(caller));
        var dto = await handler.Handle(new CreateWorkerRoleCommand(farm.Id, Req(title: " Harvester ")), CancellationToken.None);

        Assert.Equal("Harvester", Assert.Single(repo.Roles).Title);
        Assert.Equal(5000m, dto.PayRate);
    }

    [Theory]
    [InlineData(UserRole.Supervisor)]
    [InlineData(UserRole.Member)]
    public async Task Create_OtherRoles_Forbidden(UserRole caller)
    {
        var (repo, farm) = Setup();
        var handler = new CreateWorkerRoleCommandHandler(repo, new FakeFarmRepo(farm), new FakeUser(caller));
        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new CreateWorkerRoleCommand(farm.Id, Req()), CancellationToken.None));
        Assert.Empty(repo.Roles);
    }

    [Fact]
    public async Task Create_FarmOfAnotherCompany_NotFound()
    {
        var (repo, farm) = Setup();
        var handler = new CreateWorkerRoleCommandHandler(repo, new FakeFarmRepo(farm), new FakeUser(UserRole.Admin, "other-co"));
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateWorkerRoleCommand(farm.Id, Req()), CancellationToken.None));
    }

    [Theory]
    [InlineData(0, "Harvester")]
    [InlineData(-5, "Harvester")]
    [InlineData(5000, "  ")]
    public async Task Create_InvalidInput_Throws(double rate, string title)
    {
        var (repo, farm) = Setup();
        var handler = new CreateWorkerRoleCommandHandler(repo, new FakeFarmRepo(farm), new FakeUser(UserRole.Admin));
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new CreateWorkerRoleCommand(farm.Id, Req((decimal)rate, title)), CancellationToken.None));
        Assert.Empty(repo.Roles);
    }

    [Fact]
    public async Task UpdateRate_FarmManager_ChangesRate()
    {
        var (repo, farm) = Setup();
        var role = WorkerRole.Create(farm.Id, "Harvester", WorkerType.Employee, 5000, PayFrequency.Daily);
        repo.Roles.Add(role);
        var handler = new UpdateWorkerRoleRateCommandHandler(repo, new FakeFarmRepo(farm), new FakeUser(UserRole.FarmManager));

        await handler.Handle(new UpdateWorkerRoleRateCommand(role.Id, 6500, PayFrequency.Weekly), CancellationToken.None);

        Assert.Equal(6500m, role.PayRate);
        Assert.Equal(PayFrequency.Weekly, role.PayFrequency);
    }

    [Fact]
    public async Task UpdateRate_Supervisor_ForbiddenAndUnchanged()
    {
        var (repo, farm) = Setup();
        var role = WorkerRole.Create(farm.Id, "Harvester", WorkerType.Employee, 5000, PayFrequency.Daily);
        repo.Roles.Add(role);
        var handler = new UpdateWorkerRoleRateCommandHandler(repo, new FakeFarmRepo(farm), new FakeUser(UserRole.Supervisor));

        await Assert.ThrowsAsync<ForbiddenException>(() => handler.Handle(new UpdateWorkerRoleRateCommand(role.Id, 9999, PayFrequency.Daily), CancellationToken.None));
        Assert.Equal(5000m, role.PayRate);
        Assert.Equal(0, repo.UpdateCalls);
    }

    [Fact]
    public async Task UpdateRate_BandOfAnotherCompany_NotFound()
    {
        var (repo, farm) = Setup();
        var role = WorkerRole.Create(farm.Id, "Harvester", WorkerType.Employee, 5000, PayFrequency.Daily);
        repo.Roles.Add(role);
        var handler = new UpdateWorkerRoleRateCommandHandler(repo, new FakeFarmRepo(farm), new FakeUser(UserRole.Admin, "other-co"));

        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new UpdateWorkerRoleRateCommand(role.Id, 9999, PayFrequency.Daily), CancellationToken.None));
        Assert.Equal(5000m, role.PayRate);
    }
}
