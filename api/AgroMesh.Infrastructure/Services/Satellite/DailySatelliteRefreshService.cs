using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Satellite;

/// <summary>
/// Refreshes every farm's satellite readings once a day. It wakes every few hours and refreshes any farm that has not been
/// refreshed in the last 24 hours, so a restart or a missed night catches up. Does nothing until the Copernicus keys are set.
/// </summary>
public class DailySatelliteRefreshService : BackgroundService
{
    private static readonly TimeSpan CheckEvery = TimeSpan.FromHours(3);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySatelliteRefreshService> _logger;

    public DailySatelliteRefreshService(IServiceScopeFactory scopeFactory, ILogger<DailySatelliteRefreshService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try { await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); } catch (OperationCanceledException) { return; }

        while (!stoppingToken.IsCancellationRequested)
        {
            await RefreshDueFarmsAsync(stoppingToken);
            try { await Task.Delay(CheckEvery, stoppingToken); } catch (OperationCanceledException) { return; }
        }
    }

    private async Task RefreshDueFarmsAsync(CancellationToken ct)
    {
        try
        {
            List<string> due;
            using (var scope = _scopeFactory.CreateScope())
            {
                if (!scope.ServiceProvider.GetRequiredService<ISatelliteService>().IsConfigured) return;
                var farms = await scope.ServiceProvider.GetRequiredService<IFarmRepository>().GetAllAsync(ct);
                due = farms.Where(f => f.SatelliteSyncDue(DateTime.UtcNow, SatelliteRefresher.Every)).Select(f => f.Id).ToList();
            }

            foreach (var farmId in due)
            {
                try
                {
                    // A fresh scope per farm: the repositories share one DbContext, and one bad farm must not stop the rest.
                    using var scope = _scopeFactory.CreateScope();
                    var farm = await scope.ServiceProvider.GetRequiredService<IFarmRepository>().GetByIdAsync(farmId, ct);
                    if (farm is null) continue;
                    var blocks = await scope.ServiceProvider.GetRequiredService<SatelliteRefresher>().RefreshFarmAsync(farm, ct);
                    _logger.LogInformation("Daily satellite refresh: {Blocks} blocks updated", blocks);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning(ex, "Satellite refresh failed for one farm; will retry in a few hours");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Satellite refresh could not list farms");
        }
    }
}
