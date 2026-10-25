using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Block;

public record SatellitePointDto(DateTime Date, double Ndvi, double Ndmi);

public record BlockSatelliteDto(
    string BlockId, string BlockName, bool HasBoundary, string Condition, string Label, string Summary,
    DateTime? AsOf, double? Ndvi, double? Ndmi, List<SatellitePointDto> Points);

public record SatelliteOverviewDto(bool Configured, DateTime? LastSyncAt, List<BlockSatelliteDto> Blocks);

/// <summary>Crop condition from satellite for every block of the farm.</summary>
public record GetSatelliteOverviewQuery(string FarmId) : IRequest<SatelliteOverviewDto>;

public class GetSatelliteOverviewQueryHandler : IRequestHandler<GetSatelliteOverviewQuery, SatelliteOverviewDto>
{
    private const int ShowDays = 120;

    private readonly IFarmRepository _farms;
    private readonly IBlockRepository _blocks;
    private readonly IBlockSatelliteRepository _readings;
    private readonly ISatelliteService _satellite;

    public GetSatelliteOverviewQueryHandler(IFarmRepository farms, IBlockRepository blocks, IBlockSatelliteRepository readings, ISatelliteService satellite)
    {
        _farms = farms;
        _blocks = blocks;
        _readings = readings;
        _satellite = satellite;
    }

    public async Task<SatelliteOverviewDto> Handle(GetSatelliteOverviewQuery q, CancellationToken ct)
    {
        var farm = await _farms.GetByIdAsync(q.FarmId, ct) ?? throw new NotFoundException("Farm", q.FarmId);
        var blocks = await _blocks.GetByFarmIdAsync(q.FarmId, ct);
        var readings = (await _readings.GetByFarmAsync(q.FarmId, DateTime.UtcNow.Date.AddDays(-ShowDays), ct))
            .GroupBy(r => r.BlockId).ToDictionary(g => g.Key, g => g.OrderBy(r => r.Date).ToList());

        var rows = blocks.Select(b =>
        {
            var list = readings.GetValueOrDefault(b.Id) ?? [];
            var hasBoundary = SatelliteRefresher.HasUsableBoundary(b);
            if (!hasBoundary)
                return new BlockSatelliteDto(b.Id, b.Name, false, nameof(CropCondition.NoData), "No boundary",
                    SatelliteRefresher.BoundaryProblem(b) ?? "This block's boundary is not a real outline, so the satellite cannot look at it.", null, null, null, []);
            var s = SatelliteStatus.Evaluate(list);
            return new BlockSatelliteDto(b.Id, b.Name, true, s.Condition.ToString(), s.Label, s.Summary, s.AsOf, s.Ndvi, s.Ndmi,
                list.Where(r => r.ValidShare >= SatelliteStatus.ClearEnough).Select(r => new SatellitePointDto(r.Date, r.Ndvi, r.Ndmi)).ToList());
        }).ToList();

        return new SatelliteOverviewDto(_satellite.IsConfigured, farm.LastSatelliteSyncAt, rows);
    }
}
