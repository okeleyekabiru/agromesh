using AgroMesh.Application.Commands.Bank;
using AgroMesh.Application.Interfaces;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Financial;

/// <summary>
/// Pulls each linked farm's bank transactions once a day. It wakes every hour and syncs any farm that has not
/// been synced in the last 24 hours, so a restart or a missed night catches up. A person pressing "Sync now"
/// updates the same timestamp, which simply pushes the next automatic sync a day out.
/// </summary>
public class DailyBankSyncService : BackgroundService
{
    private static readonly TimeSpan CheckEvery = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailyBankSyncService> _logger;

    public DailyBankSyncService(IServiceScopeFactory scopeFactory, ILogger<DailyBankSyncService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the app a moment to finish starting (migrations run on startup).
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await SyncDueFarmsAsync(stoppingToken);
            try { await Task.Delay(CheckEvery, stoppingToken); } catch (OperationCanceledException) { return; }
        }
    }

    private async Task SyncDueFarmsAsync(CancellationToken ct)
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
                    _logger.LogWarning(ex, "Daily bank sync failed for one account; will retry next hour");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Daily bank sync could not list farms");
        }
    }
}
