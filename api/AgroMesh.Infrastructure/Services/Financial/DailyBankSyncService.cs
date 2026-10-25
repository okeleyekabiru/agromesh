using Microsoft.Extensions.Hosting;

namespace AgroMesh.Infrastructure.Services.Financial;

/// <summary>
/// Local/dev fallback: wakes every hour and runs <see cref="DailyBankSyncRunner"/>, so a restart or a missed night
/// catches up. In production this is a backstop, not the primary trigger, since a host that spins down when idle
/// (e.g. Render's free tier) won't keep this loop alive reliably — the daily-jobs endpoint, called by an external
/// scheduler, is the primary trigger there. A person pressing "Sync now" updates the same timestamp, which simply
/// pushes the next automatic sync a day out.
/// </summary>
public class DailyBankSyncService : BackgroundService
{
    private static readonly TimeSpan CheckEvery = TimeSpan.FromHours(1);

    private readonly DailyBankSyncRunner _runner;

    public DailyBankSyncService(DailyBankSyncRunner runner)
    {
        _runner = runner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Give the app a moment to finish starting (migrations run on startup).
        try { await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await _runner.RunAsync(stoppingToken);
            try { await Task.Delay(CheckEvery, stoppingToken); } catch (OperationCanceledException) { return; }
        }
    }
}
