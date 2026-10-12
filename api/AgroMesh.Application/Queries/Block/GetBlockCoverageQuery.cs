using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Block;

/// <summary>What has been done and what is left of one kind of work on a block, in its current cycle.</summary>
public record BlockCoverageDto(
    string BlockId,
    string BlockName,
    ActivityType ActivityType,
    string Status,                 // NotStarted | InProgress | Complete
    bool HasBoundary,
    double BlockHectares,
    double CoveredHectares,
    double? RemainingHectares,
    double Percent,
    DateTime? CycleStartedOn,
    DateTime? LastWorkedOn,
    int ActivityCount,
    string? BlockGeoJson,
    string? CoveredGeoJson,
    string? RemainingGeoJson,
    List<CoverageSectionDto> Sections);

/// <summary>One job's marked area in the current cycle, numbered oldest first.</summary>
public record CoverageSectionDto(int Number, DateTime Date, double Hectares, string GeoJson);

public record GetBlockCoverageQuery(string FarmId, string BlockId, ActivityType ActivityType) : IRequest<BlockCoverageDto>;

public class GetBlockCoverageQueryHandler : IRequestHandler<GetBlockCoverageQuery, BlockCoverageDto>
{
    private const int MaxActivities = 200;

    private readonly IBlockRepository _blocks;
    private readonly IActivityRepository _activities;
    private readonly ICoverageCompletionRepository _completions;

    public GetBlockCoverageQueryHandler(IBlockRepository blocks, IActivityRepository activities, ICoverageCompletionRepository completions)
    {
        _blocks = blocks;
        _activities = activities;
        _completions = completions;
    }

    public async Task<BlockCoverageDto> Handle(GetBlockCoverageQuery q, CancellationToken ct)
    {
        var block = await _blocks.GetByIdAsync(q.BlockId, ct);
        if (block is null || block.FarmId != q.FarmId)
            throw new NotFoundException("Block", q.BlockId);

        var logged = await _activities.GetRecentByBlockAsync(block.Id, q.ActivityType, MaxActivities, ct);
        // A manager can mark a cycle finished by hand; that counts as the whole block at that moment.
        var forced = await _completions.GetByBlockAsync(block.Id, q.ActivityType, ct);
        var result = WorkCoverage.Compute(block.BoundaryGeoJson,
            logged.Select(a => (a.Date, a.WorkedAreaGeoJson)).Concat(forced.Select(c => (c.CompletedOn, (string?)null))));

        return new BlockCoverageDto(
            block.Id, block.Name, q.ActivityType, result.Status.ToString(), result.HasBoundary,
            result.HasBoundary ? result.BlockHectares : 0, result.CoveredHectares, result.RemainingHectares,
            result.Percent, result.CycleStartedOn, result.LastWorkedOn, result.ActivityCount,
            result.HasBoundary ? block.BoundaryGeoJson : null, result.CoveredGeoJson, result.RemainingGeoJson,
            (result.Sections ?? []).Select((s, i) => new CoverageSectionDto(i + 1, s.Date, s.Hectares, s.GeoJson)).ToList());
    }
}
