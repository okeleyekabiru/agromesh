using AgroMesh.Application.Commands.Bank;
using AgroMesh.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Financial;

/// <summary>
/// Syncs every farm whose bank account is due. Shared by <see cref="DailyBankSyncService"/>'s hourly in-process
/// loop and by an external trigger (e.g. a Vercel cron hitting the daily-jobs endpoint), so the sync still happens
/// on a host that doesn't stay resident between requests.
/// </summary>
public class DailyBankSyncRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyBankSyncRunner> _logger;

    public DailyBankSyncRunner(IServiceScopeFactory scopeFactory, ILogger<DailyBankSyncRunner> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken ct)
    {
        try
        {
            List<string> due;
            using (var scope = _scopeFactory.CreateScope())
            {
                var farms = await scope.ServiceProvider.GetRequiredService<IFarmRepository>().GetAllAsync(ct);
                due = farms.Where(f => f.BankSyncDue(DateTime.UtcNow)).Select(f => f.BankAccountId!).ToList();
            }

            foreach (var accountId in due)
            {
                try
                {
                    // A fresh scope per farm: the repositories share one DbContext, and one bad account must not stop the rest.
                    using var scope = _scopeFactory.CreateScope();
                    var added = await scope.ServiceProvider.GetRequiredService<IMediator>()
                        .Send(new SyncBankAccountByIdCommand(accountId), ct);
                    _logger.LogInformation("Daily bank sync: {Added} new transactions", added);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Daily bank sync failed for one account; will retry next run");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Daily bank sync could not list farms");
        }
    }
}
