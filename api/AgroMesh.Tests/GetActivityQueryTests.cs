using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Activity;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class GetActivityQueryTests
{
    private class FakeActivityRepo(ActivityLog? activity) : IActivityRepository
    {
        public Task<ActivityLog?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(activity is not null && activity.Id == id ? activity : null);
        public Task<(List<ActivityLog> Items, int Total)> GetByFarmIdAsync(string farmId, int page, int pageSize, CancellationToken ct = default) => Task.FromResult((new List<ActivityLog>(), 0));
        public Task<List<ActivityLog>> GetRecentByBlockAsync(string blockId, ActivityType activityType, int limit = 5, CancellationToken ct = default) => Task.FromResult(new List<ActivityLog>());
        public Task<List<ActivityLog>> GetRecentByFarmIdAsync(string farmId, int limit = 10, CancellationToken ct = default) => Task.FromResult(new List<ActivityLog>());
        public Task<ActivityLog> AddAsync(ActivityLog a, CancellationToken ct = default) => Task.FromResult(a);
        public Task<int> GetCountThisMonthAsync(string farmId, CancellationToken ct = default) => Task.FromResult(0);
        public Task<decimal> GetLabourCostMtdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(0m);
        public Task<List<ActivityLog>> GetBetweenAsync(string farmId, DateTime from, DateTime to, CancellationToken ct = default) => Task.FromResult(new List<ActivityLog>());
        public Task UpdateAsync(ActivityLog activity, CancellationToken ct = default) => Task.CompletedTask;
        public Task<List<ActivityLog>> GetUnpricedUsingItemAsync(string farmId, string inventoryItemId, CancellationToken ct = default) => Task.FromResult(new List<ActivityLog>());
        public Task<List<MonthAmount>> GetMonthlyCostsAsync(string farmId, DateTime from, CancellationToken ct = default) => Task.FromResult(new List<MonthAmount>());
        public Task<List<BlockCostRow>> GetBlockCostsMtdAsync(string farmId, CancellationToken ct = default) => Task.FromResult(new List<BlockCostRow>());
    }

    private class FakeFarmRepo(params Farm[] farms) : IFarmRepository
    {
        public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(farms.FirstOrDefault(f => f.Id == id));
        public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
        public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(farms.ToList());
        public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) => Task.FromResult(new List<Farm>());
        public Task<Farm> AddAsync(Farm f, CancellationToken ct = default) => Task.FromResult(f);
        public Task UpdateAsync(Farm f, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(farms.Any(f => f.Id == id));
    }

    private class FakeUser(string? companyId = "co-1") : ICurrentUserAccessor
    {
        public string? UserId => "u1";
        public string? CompanyId => companyId;
        public UserRole Role => UserRole.Admin;
    }

    private static ActivityLog MakeActivity(string farmId) => ActivityLog.Create(
        farmId, "block1", ActivityType.Spraying, DateTime.UtcNow,
        [new WorkerLog("w1", "Ade", WorkerType.Employee, 4, 5000, PayFrequency.Daily)],
        [new InputUsed("Glyphosate", 2, "L")], "sup1", notes: "North edge");

    [Fact]
    public async Task ReturnsFullActivity()
    {
        var farm = Farm.Create("F", "co-1", "owner");
        var activity = MakeActivity(farm.Id);
        var handler = new GetActivityQueryHandler(new FakeActivityRepo(activity), new FakeFarmRepo(farm), new FakeUser());

        var dto = await handler.Handle(new GetActivityQuery(farm.Id, activity.Id), CancellationToken.None);

        Assert.Equal(activity.Id, dto.Id);
        Assert.Equal("North edge", dto.Notes);
        Assert.Single(dto.Workers);
        Assert.Single(dto.Inputs);
        Assert.Equal(2500m, dto.LabourCost);
    }

    [Fact]
    public async Task ActivityOfAnotherFarm_IsNotFound()
    {
        var farm = Farm.Create("F", "co-1", "owner");
        var otherFarm = Farm.Create("Other", "co-1", "owner");
        var activity = MakeActivity(otherFarm.Id);
        var handler = new GetActivityQueryHandler(new FakeActivityRepo(activity), new FakeFarmRepo(farm, otherFarm), new FakeUser());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetActivityQuery(farm.Id, activity.Id), CancellationToken.None));
    }

    [Fact]
    public async Task FarmOfAnotherCompany_IsNotFound()
    {
        var farm = Farm.Create("F", "co-1", "owner");
        var activity = MakeActivity(farm.Id);
        var handler = new GetActivityQueryHandler(new FakeActivityRepo(activity), new FakeFarmRepo(farm), new FakeUser("other-co"));

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetActivityQuery(farm.Id, activity.Id), CancellationToken.None));
    }

    [Fact]
    public async Task UnknownActivity_IsNotFound()
    {
        var farm = Farm.Create("F", "co-1", "owner");
        var handler = new GetActivityQueryHandler(new FakeActivityRepo(null), new FakeFarmRepo(farm), new FakeUser());

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetActivityQuery(farm.Id, "nope"), CancellationToken.None));
    }
}
