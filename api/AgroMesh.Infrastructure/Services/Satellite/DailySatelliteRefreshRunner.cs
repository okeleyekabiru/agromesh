using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Satellite;

/// <summary>
/// Refreshes every farm's satellite readings that are due. Shared by <see cref="DailySatelliteRefreshService"/>'s
/// in-process loop and by an external trigger (e.g. a Vercel cron hitting the daily-jobs endpoint), so the refresh
/// still happens on a host that doesn't stay resident between requests. Does nothing until the Copernicus keys are set.
/// </summary>
public class DailySatelliteRefreshRunner
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DailySatelliteRefreshRunner> _logger;

    public DailySatelliteRefreshRunner(IServiceScopeFactory scopeFactory, ILogger<DailySatelliteRefreshRunner> logger)
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
                    _logger.LogWarning(ex, "Satellite refresh failed for one farm; will retry next run");
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Satellite refresh could not list farms");
        }
    }
}
