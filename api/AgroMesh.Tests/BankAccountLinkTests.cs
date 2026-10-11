using AgroMesh.Application.Commands.Bank;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class BankAccountLinkTests
{
    private class FakeMono : IMonoBankService
    {
        public string AccountId = "acc-1";
        public int Added = 3;
        public bool Unlinked;
        public Task<string> ExchangeCodeAsync(string code, CancellationToken ct = default) => Task.FromResult(AccountId);
        public Task<int> SyncTransactionsAsync(string farmId, string monoAccountId, CancellationToken ct = default) => Task.FromResult(Added);
        public Task UnlinkAsync(string monoAccountId, CancellationToken ct = default) { Unlinked = true; return Task.CompletedTask; }
    }

    private class FakeUser : ICurrentUserAccessor
    {
        public string? UserId => "u";
        public string? CompanyId => "co-1";
        public AgroMesh.Domain.Enums.UserRole Role => AgroMesh.Domain.Enums.UserRole.Admin;
    }

    private class FakeFarms(params Farm[] farms) : IFarmRepository
    {
        public Task<Farm?> GetByIdAsync(string id, CancellationToken ct = default) => Task.FromResult(farms.FirstOrDefault(f => f.Id == id));
        public Task<Farm?> GetByIdWithBlocksAsync(string id, CancellationToken ct = default) => GetByIdAsync(id, ct);
        public Task<List<Farm>> GetAllAsync(CancellationToken ct = default) => Task.FromResult(farms.ToList());
        public Task<List<Farm>> GetByOwnerIdAsync(string ownerId, CancellationToken ct = default) => Task.FromResult(farms.ToList());
        public Task<List<Farm>> GetByCompanyIdAsync(string companyId, CancellationToken ct = default) => Task.FromResult(farms.ToList());
        public Task<Farm> AddAsync(Farm farm, CancellationToken ct = default) => Task.FromResult(farm);
        public Task UpdateAsync(Farm farm, CancellationToken ct = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string id, CancellationToken ct = default) => Task.FromResult(farms.Any(f => f.Id == id));
    }

    private static Farm NewFarm(string company = "co-1") => Farm.Create("F", company, "owner");

    [Fact]
    public async Task Linking_stores_the_account_and_pulls_the_transactions()
    {
        var farm = NewFarm();
        var handler = new LinkBankAccountHandler(new FakeFarms(farm), new FakeUser(), new FakeMono());

        var status = await handler.Handle(new LinkBankAccountCommand(farm.Id, "code-123"), default);

        Assert.True(status.Linked);
        Assert.Equal("acc-1", farm.BankAccountId);
        Assert.Equal(3, status.NewTransactions);
        Assert.NotNull(farm.LastBankSyncAt);
    }

    [Fact]
    public async Task Linking_without_a_code_is_rejected()
    {
        var farm = NewFarm();
        var handler = new LinkBankAccountHandler(new FakeFarms(farm), new FakeUser(), new FakeMono());
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new LinkBankAccountCommand(farm.Id, " "), default));
    }

    [Fact]
    public async Task Another_companys_farm_cannot_be_linked()
    {
        var farm = NewFarm("someone-else");
        var handler = new LinkBankAccountHandler(new FakeFarms(farm), new FakeUser(), new FakeMono());
        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new LinkBankAccountCommand(farm.Id, "code"), default));
    }

    [Fact]
    public async Task Syncing_needs_a_linked_account()
    {
        var farm = NewFarm();
        var handler = new SyncBankAccountHandler(new FakeFarms(farm), new FakeUser(), new FakeMono());
        await Assert.ThrowsAsync<DomainException>(() => handler.Handle(new SyncBankAccountCommand(farm.Id), default));
    }

    [Fact]
    public async Task Unlinking_forgets_the_account_and_tells_the_bank()
    {
        var farm = NewFarm();
        farm.LinkBankAccount("acc-1");
        var mono = new FakeMono();
        var status = await new UnlinkBankAccountHandler(new FakeFarms(farm), new FakeUser(), mono)
            .Handle(new UnlinkBankAccountCommand(farm.Id), default);

        Assert.False(status.Linked);
        Assert.Null(farm.BankAccountId);
        Assert.True(mono.Unlinked);
    }

    [Fact]
    public async Task A_webhook_for_a_known_account_syncs_that_farm_only()
    {
        var farm = NewFarm();
        farm.LinkBankAccount("acc-9");
        var handler = new SyncBankAccountByIdHandler(new FakeFarms(farm, NewFarm()), new FakeMono { Added = 2 });

        Assert.Equal(2, await handler.Handle(new SyncBankAccountByIdCommand("acc-9"), default));
        Assert.Equal(0, await handler.Handle(new SyncBankAccountByIdCommand("unknown"), default));
    }

    [Fact]
    public void A_linked_farm_is_synced_once_a_day()
    {
        var now = new DateTime(2026, 10, 12, 12, 0, 0, DateTimeKind.Utc);
        var farm = NewFarm();
        Assert.False(farm.BankSyncDue(now));                  // nothing linked: nothing to sync

        farm.LinkBankAccount("acc-1");
        Assert.True(farm.BankSyncDue(now));                   // never synced

        farm.MarkBankSynced();
        var justSynced = DateTime.UtcNow;
        Assert.False(farm.BankSyncDue(justSynced.AddHours(23)));  // synced today: wait
        Assert.True(farm.BankSyncDue(justSynced.AddHours(24)));   // a day later: due again
    }
}
