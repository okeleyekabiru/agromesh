using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Bank;

public record BankAccountStatusDto(bool Linked, DateTime? LinkedAt, DateTime? LastSyncAt, int? NewTransactions = null);

public record LinkBankAccountCommand(string FarmId, string Code) : IRequest<BankAccountStatusDto>;
public record SyncBankAccountCommand(string FarmId) : IRequest<BankAccountStatusDto>;
public record UnlinkBankAccountCommand(string FarmId) : IRequest<BankAccountStatusDto>;
public record GetBankAccountQuery(string FarmId) : IRequest<BankAccountStatusDto>;

internal static class BankHelpers
{
    public static async Task<Domain.Entities.Farm> FarmAsync(IFarmRepository repo, ICurrentUserAccessor user, string farmId, CancellationToken ct)
    {
        var farm = await repo.GetByIdAsync(farmId, ct) ?? throw new NotFoundException("Farm", farmId);
        if (!string.IsNullOrEmpty(user.CompanyId) && farm.CompanyId != user.CompanyId)
            throw new NotFoundException("Farm", farmId);
        return farm;
    }

    public static BankAccountStatusDto Status(Domain.Entities.Farm f, int? added = null) =>
        new(f.BankAccountId is not null, f.BankLinkedAt, f.LastBankSyncAt, added);
}

public class LinkBankAccountHandler : IRequestHandler<LinkBankAccountCommand, BankAccountStatusDto>
{
    private readonly IFarmRepository _farms;
    private readonly ICurrentUserAccessor _user;
    private readonly IMonoBankService _mono;

    public LinkBankAccountHandler(IFarmRepository farms, ICurrentUserAccessor user, IMonoBankService mono)
    {
        _farms = farms; _user = user; _mono = mono;
    }

    public async Task<BankAccountStatusDto> Handle(LinkBankAccountCommand c, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(c.Code)) throw new DomainException("The bank connection code is missing.");
        var farm = await BankHelpers.FarmAsync(_farms, _user, c.FarmId, ct);

        var accountId = await _mono.ExchangeCodeAsync(c.Code, ct);
        farm.LinkBankAccount(accountId);
        await _farms.UpdateAsync(farm, ct);

        // Pull what the bank already has. If Mono is still preparing the data, a later sync or webhook fills it in.
        var added = await _mono.SyncTransactionsAsync(farm.Id, accountId, ct);
        farm.MarkBankSynced();
        await _farms.UpdateAsync(farm, ct);
        return BankHelpers.Status(farm, added);
    }
}

public class SyncBankAccountHandler : IRequestHandler<SyncBankAccountCommand, BankAccountStatusDto>
{
    private readonly IFarmRepository _farms;
    private readonly ICurrentUserAccessor _user;
    private readonly IMonoBankService _mono;

    public SyncBankAccountHandler(IFarmRepository farms, ICurrentUserAccessor user, IMonoBankService mono)
    {
        _farms = farms; _user = user; _mono = mono;
    }

    public async Task<BankAccountStatusDto> Handle(SyncBankAccountCommand c, CancellationToken ct)
    {
        var farm = await BankHelpers.FarmAsync(_farms, _user, c.FarmId, ct);
        if (farm.BankAccountId is null) throw new DomainException("No bank account is linked to this farm.");

        var added = await _mono.SyncTransactionsAsync(farm.Id, farm.BankAccountId, ct);
        farm.MarkBankSynced();
        await _farms.UpdateAsync(farm, ct);
        return BankHelpers.Status(farm, added);
    }
}

public class UnlinkBankAccountHandler : IRequestHandler<UnlinkBankAccountCommand, BankAccountStatusDto>
{
    private readonly IFarmRepository _farms;
    private readonly ICurrentUserAccessor _user;
    private readonly IMonoBankService _mono;

    public UnlinkBankAccountHandler(IFarmRepository farms, ICurrentUserAccessor user, IMonoBankService mono)
    {
        _farms = farms; _user = user; _mono = mono;
    }

    public async Task<BankAccountStatusDto> Handle(UnlinkBankAccountCommand c, CancellationToken ct)
    {
        var farm = await BankHelpers.FarmAsync(_farms, _user, c.FarmId, ct);
        if (farm.BankAccountId is { } id) await _mono.UnlinkAsync(id, ct);
        farm.UnlinkBankAccount();
        await _farms.UpdateAsync(farm, ct);
        return BankHelpers.Status(farm);
    }
}

public class GetBankAccountHandler : IRequestHandler<GetBankAccountQuery, BankAccountStatusDto>
{
    private readonly IFarmRepository _farms;
    private readonly ICurrentUserAccessor _user;

    public GetBankAccountHandler(IFarmRepository farms, ICurrentUserAccessor user) { _farms = farms; _user = user; }

    public async Task<BankAccountStatusDto> Handle(GetBankAccountQuery q, CancellationToken ct) =>
        BankHelpers.Status(await BankHelpers.FarmAsync(_farms, _user, q.FarmId, ct));
}

/// <summary>Mono says an account changed: pull its new transactions. Unknown accounts are ignored.</summary>
public record SyncBankAccountByIdCommand(string MonoAccountId) : IRequest<int>;

public class SyncBankAccountByIdHandler : IRequestHandler<SyncBankAccountByIdCommand, int>
{
    private readonly IFarmRepository _farms;
    private readonly IMonoBankService _mono;

    public SyncBankAccountByIdHandler(IFarmRepository farms, IMonoBankService mono) { _farms = farms; _mono = mono; }

    public async Task<int> Handle(SyncBankAccountByIdCommand c, CancellationToken ct)
    {
        var farm = (await _farms.GetAllAsync(ct)).FirstOrDefault(f => f.BankAccountId == c.MonoAccountId);
        if (farm is null) return 0;

        var added = await _mono.SyncTransactionsAsync(farm.Id, c.MonoAccountId, ct);
        farm.MarkBankSynced();
        await _farms.UpdateAsync(farm, ct);
        return added;
    }
}
