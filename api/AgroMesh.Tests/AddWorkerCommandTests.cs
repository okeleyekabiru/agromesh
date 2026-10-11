using AgroMesh.Application.Commands.Worker;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class AddWorkerCommandTests
{
    private class FakeWorkerRepo : IWorkerRepository
    {
        public List<Worker> Workers = [];
        public Task<Worker?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(Workers.FirstOrDefault(w => w.Id == id));
        public Task<Worker?> GetByIdWithRoleAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
        public Task<List<Worker>> GetByFarmIdAsync(string farmId, bool activeOnly = true, CancellationToken ct = default) => Task.FromResult(Workers.Where(w => w.FarmId == farmId).ToList());
        public Task<Worker> AddAsync(Worker worker, CancellationToken ct = default) { Workers.Add(worker); return Task.FromResult(worker); }
        public Task UpdateAsync(Worker worker, CancellationToken ct = default) => Task.CompletedTask;
        public Task<int> GetCountByRoleAsync(string workerRoleId, CancellationToken ct = default) => Task.FromResult(0);
    }

    private class FakeRoleRepo(WorkerRole? role) : IWorkerRoleRepository
    {
        public Task<WorkerRole?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(role is not null && role.Id == id ? role : null);
        public Task<List<WorkerRole>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(new List<WorkerRole>());
        public Task<WorkerRole> AddAsync(WorkerRole r, CancellationToken ct = default) => Task.FromResult(r);
        public Task UpdateAsync(WorkerRole r, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(role?.Id == id);
    }

    private class FakeFarmRepo(Farm? farm) : IFarmRepository
    {
        public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(farm is not null && farm.Id == id ? farm : null);
        public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
        public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<Farm> AddAsync(Farm f, CancellationToken ct = default) => Task.FromResult(f);
        public Task UpdateAsync(Farm f, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(farm?.Id == id);
    }

    private class FakeUser(UserRole role, string? companyId = "co-1") : ICurrentUserAccessor
    {
        public string? UserId => "u1";
        public string? CompanyId => companyId;
        public UserRole Role => role;
    }

    private static (AddWorkerCommandHandler handler, FakeWorkerRepo repo, Farm farm, WorkerRole role) Create(UserRole caller, string callerCompany = "co-1")
    {
        var farm = Farm.Create("Farm", "co-1", "owner");
        var role = WorkerRole.Create(farm.Id, "Harvester", WorkerType.Employee, 5000, PayFrequency.Daily);
        var repo = new FakeWorkerRepo();
        return (new AddWorkerCommandHandler(repo, new FakeRoleRepo(role), new FakeFarmRepo(farm), new FakeUser(caller, callerCompany)), repo, farm, role);
    }

    [Theory]
    [InlineData(UserRole.Admin)]
    [InlineData(UserRole.FarmManager)]
    [InlineData(UserRole.Supervisor)]
    public async Task AllowedRoles_AddWorker(UserRole caller)
    {
        var (handler, repo, farm, role) = Create(caller);
        var dto = await handler.Handle(new AddWorkerCommand(farm.Id, new AddWorkerRequest(" Ade Bello ", "AB", role.Id, WorkerType.Employee)), CancellationToken.None);

        Assert.Equal("Ade Bello", Assert.Single(repo.Workers).Name);
        Assert.Equal("Harvester", dto.RoleTitle);
    }

    [Fact]
    public async Task MemberRole_IsForbidden()
    {
        var (handler, repo, farm, role) = Create(UserRole.Member);
        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(new AddWorkerCommand(farm.Id, new AddWorkerRequest("A", "A", role.Id, WorkerType.Employee)), CancellationToken.None));
        Assert.Empty(repo.Workers);
    }

    [Fact]
    public async Task FarmOfAnotherCompany_IsNotFound()
    {
        var (handler, repo, farm, role) = Create(UserRole.Admin, callerCompany: "other-co");
        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new AddWorkerCommand(farm.Id, new AddWorkerRequest("A", "A", role.Id, WorkerType.Employee)), CancellationToken.None));
        Assert.Empty(repo.Workers);
    }

    [Fact]
    public async Task WageBandFromAnotherFarm_Throws()
    {
        var (handler, repo, farm, _) = Create(UserRole.Admin);
        var foreign = WorkerRole.Create("other-farm", "X", WorkerType.Employee, 1, PayFrequency.Daily);
        var h = new AddWorkerCommandHandler(repo, new FakeRoleRepo(foreign), new FakeFarmRepo(farm), new FakeUser(UserRole.Admin));
        await Assert.ThrowsAsync<DomainException>(() =>
            h.Handle(new AddWorkerCommand(farm.Id, new AddWorkerRequest("A", "A", foreign.Id, WorkerType.Employee)), CancellationToken.None));
    }

    [Fact]
    public async Task BlankName_Throws()
    {
        var (handler, _, farm, role) = Create(UserRole.Admin);
        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(new AddWorkerCommand(farm.Id, new AddWorkerRequest("  ", "A", role.Id, WorkerType.Employee)), CancellationToken.None));
    }
}
