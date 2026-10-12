using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Block;

public record SatelliteDateDto(DateTime Date, double Ndvi, double Ndmi, double ClearShare);

public record BlockSatelliteViewDto(
    string BlockId, string BlockName, bool Configured, bool HasBoundary,
    double[]? Bounds, // [minLat, minLng, maxLat, maxLng]
    List<SatelliteDateDto> Dates);

/// <summary>The dates a block has a satellite picture for (newest first), and the box to place a picture in.</summary>
public record GetBlockSatelliteViewQuery(string FarmId, string BlockId) : IRequest<BlockSatelliteViewDto>;

public class GetBlockSatelliteViewQueryHandler : IRequestHandler<GetBlockSatelliteViewQuery, BlockSatelliteViewDto>
{
    public const int ShowDays = 150;

    private readonly IBlockRepository _blocks;
    private readonly IBlockSatelliteRepository _readings;
    private readonly ISatelliteService _satellite;

    public GetBlockSatelliteViewQueryHandler(IBlockRepository blocks, IBlockSatelliteRepository readings, ISatelliteService satellite)
    {
        _blocks = blocks;
        _readings = readings;
        _satellite = satellite;
    }

    public async Task<BlockSatelliteViewDto> Handle(GetBlockSatelliteViewQuery q, CancellationToken ct)
    {
        var block = await _blocks.GetByIdAsync(q.BlockId, ct);
        if (block is null || block.FarmId != q.FarmId) throw new NotFoundException("Block", q.BlockId);

        var usable = SatelliteRefresher.HasUsableBoundary(block);
        var box = usable ? GeoBounds.Of(block.BoundaryGeoJson) : null;
        var dates = usable
            ? (await _readings.GetByBlockAsync(block.Id, DateTime.UtcNow.Date.AddDays(-ShowDays), ct))
                .OrderByDescending(r => r.Date)
                .Select(r => new SatelliteDateDto(r.Date, r.Ndvi, r.Ndmi, r.ValidShare)).ToList()
            : [];

        return new BlockSatelliteViewDto(block.Id, block.Name, _satellite.IsConfigured, usable,
            box is null ? null : [box.MinLat, box.MinLng, box.MaxLat, box.MaxLng], dates);
    }
}

/// <summary>One picture of a block: a layer for the 5-day window that starts on <paramref name="Date"/>.</summary>
public record GetBlockSatelliteImageQuery(string FarmId, string BlockId, SatelliteLayer Layer, DateTime Date) : IRequest<byte[]>;

public class GetBlockSatelliteImageQueryHandler : IRequestHandler<GetBlockSatelliteImageQuery, byte[]>
{
    private const int WindowDays = 5, MaxCache = 300;
    private const double MetersPerPixel = 10, Upscale = 3;   // Sentinel-2 is 10 m; drawn 3x so it is smooth, not blocky
    private const int MinSide = 64, MaxSide = 1500;

    // Pictures never change once the satellite has passed, so keep them for a while instead of using the free quota again.
    private static readonly Dictionary<string, byte[]> Cache = [];
    private static readonly object CacheLock = new();

    private readonly IBlockRepository _blocks;
    private readonly ISatelliteService _satellite;

    public GetBlockSatelliteImageQueryHandler(IBlockRepository blocks, ISatelliteService satellite)
    {
        _blocks = blocks;
        _satellite = satellite;
    }

    public async Task<byte[]> Handle(GetBlockSatelliteImageQuery q, CancellationToken ct)
    {
        var block = await _blocks.GetByIdAsync(q.BlockId, ct);
        if (block is null || block.FarmId != q.FarmId) throw new NotFoundException("Block", q.BlockId);
        if (!_satellite.IsConfigured) throw new DomainException("Satellite monitoring is not connected yet.");
        if (!SatelliteRefresher.HasUsableBoundary(block) || GeoBounds.Of(block.BoundaryGeoJson) is not { } box)
            throw new DomainException("This block has no usable boundary, so there is no picture to show. Redraw it on the Blocks page.");

        var day = q.Date.Date;
        var today = DateTime.UtcNow.Date;
        if (day > today || day < today.AddDays(-GetBlockSatelliteViewQueryHandler.ShowDays - WindowDays))
            throw new DomainException("That date is outside the range of available pictures.");

        var key = $"{block.Id}|{block.BoundaryGeoJson!.GetHashCode()}|{q.Layer}|{day:yyyyMMdd}";
        lock (CacheLock) { if (Cache.TryGetValue(key, out var hit)) return hit; }

        int Side(double meters) => (int)Math.Clamp(Math.Ceiling(meters / MetersPerPixel * Upscale), MinSide, MaxSide);
        var png = await _satellite.GetImageAsync(block.BoundaryGeoJson!, q.Layer,
            DateTime.SpecifyKind(day, DateTimeKind.Utc), DateTime.SpecifyKind(day.AddDays(WindowDays), DateTimeKind.Utc),
            Side(box.WidthMeters), Side(box.HeightMeters), ct);

        lock (CacheLock)
        {
            if (Cache.Count >= MaxCache) Cache.Clear();
            Cache[key] = png;
        }
        return png;
    }
}
