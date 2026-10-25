using Microsoft.Extensions.Hosting;

namespace AgroMesh.Infrastructure.Services.Satellite;

/// <summary>
/// Local/dev fallback: wakes every few hours and runs <see cref="DailySatelliteRefreshRunner"/>, so a restart or a
/// missed night catches up. In production this is a backstop, not the primary trigger, since a host that spins down
/// when idle (e.g. Render's free tier) won't keep this loop alive reliably — the daily-jobs endpoint, called by an
/// external scheduler, is the primary trigger there.
/// </summary>
public class DailySatelliteRefreshService : BackgroundService
{
    private static readonly TimeSpan CheckEvery = TimeSpan.FromHours(3);

    private readonly DailySatelliteRefreshRunner _runner;

    public DailySatelliteRefreshService(DailySatelliteRefreshRunner runner)
    {
        _runner = runner;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await _runner.RunAsync(stoppingToken);
            try { await Task.Delay(CheckEvery, stoppingToken); } catch (OperationCanceledException) { return; }
        }
    }
}
