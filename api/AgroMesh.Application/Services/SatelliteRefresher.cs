using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Application.Services;

/// <summary>Fetches the latest satellite readings for a farm's blocks and saves them. Only blocks with a measurable outline are asked about.</summary>
public class SatelliteRefresher
{
    public static readonly TimeSpan Every = TimeSpan.FromHours(24);
    public static readonly TimeSpan ManualEvery = TimeSpan.FromHours(1);
    private const int LookBackDays = 90;
    private const double MinHectares = 0.01, MaxHectares = 5000;

    private readonly ISatelliteService _satellite;
    private readonly IBlockRepository _blocks;
    private readonly IBlockSatelliteRepository _readings;
    private readonly IFarmRepository _farms;
    private readonly ILogger<SatelliteRefresher> _logger;

    public SatelliteRefresher(ISatelliteService satellite, IBlockRepository blocks, IBlockSatelliteRepository readings, IFarmRepository farms, ILogger<SatelliteRefresher> logger)
    {
        _satellite = satellite;
        _blocks = blocks;
        _readings = readings;
        _farms = farms;
        _logger = logger;
    }

    /// <summary>True when the block's outline is real: it has an area a farm could have. A boundary of one repeated point has none.</summary>
    public static bool HasUsableBoundary(Block block)
    {
        var c = WorkCoverage.Compute(block.BoundaryGeoJson, []);
        return c.HasBoundary && c.BlockHectares is >= MinHectares and <= MaxHectares;
    }

    /// <summary>Returns the number of blocks that got new numbers. Never throws for one block's failure.</summary>
    public async Task<int> RefreshFarmAsync(Farm farm, CancellationToken ct)
    {
        if (!_satellite.IsConfigured) return 0;

        var today = DateTime.UtcNow.Date;
        var updated = 0;
        // One block at a time: the repositories share a single DbContext.
        foreach (var block in (await _blocks.GetByFarmIdAsync(farm.Id, ct)).Where(b => b.Status == BlockStatus.Active && HasUsableBoundary(b)))
        {
            try
            {
                var since = today.AddDays(-LookBackDays);
                var have = await _readings.GetByBlockAsync(block.Id, since, ct);
                // Re-fetch a little before the newest reading: its window may have been cloudy when it was first seen.
                var from = have.Count > 0 ? have.Max(r => r.Date).AddDays(-10) : since;
                if (from < since) from = since;

                var observations = await _satellite.GetBlockStatsAsync(block.BoundaryGeoJson!, from, today.AddDays(1), ct);
                if (observations.Count == 0) continue;
                await _readings.UpsertAsync(farm.Id, block.Id, observations, ct);
                updated++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Satellite readings failed for block {BlockId}; will try again next time", block.Id);
            }
        }

        farm.MarkSatelliteSynced();
        await _farms.UpdateAsync(farm, ct);
        return updated;
    }
}
