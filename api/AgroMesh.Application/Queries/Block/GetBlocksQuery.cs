using AgroMesh.Application.Commands.Block;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Block;

public record GetBlocksQuery(string FarmId) : IRequest<List<BlockDto>>;

public class GetBlocksQueryHandler : IRequestHandler<GetBlocksQuery, List<BlockDto>>
{
    private readonly IBlockRepository _blockRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly BlockCycleService _cycles;
    private readonly ICurrentUserAccessor _currentUser;

    public GetBlocksQueryHandler(
        IBlockRepository blockRepo,
        IFarmRepository farmRepo,
        BlockCycleService cycles,
        ICurrentUserAccessor currentUser)
    {
        _blockRepo = blockRepo;
        _farmRepo = farmRepo;
        _cycles = cycles;
        _currentUser = currentUser;
    }

    public async Task<List<BlockDto>> Handle(GetBlocksQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new NotFoundException("Farm", query.FarmId);

        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", query.FarmId);

        var blocks = await _blockRepo.GetByFarmIdAsync(query.FarmId, ct);
        var (cycles, weatherKnown) = await _cycles.ComputeAsync(farm, blocks, ct);

        return blocks.Select(s =>
        {
            var cycle = cycles[s.Id];
            return CreateBlockCommandHandler.MapToDto(s, farm.PreferredAreaUnit) with
            {
                NextWeedingPrediction = BlockCycleService.ToTask(s, ActivityType.Weeding, cycle.Weeding, weatherKnown),
                NextSprayingPrediction = BlockCycleService.ToTask(s, ActivityType.Spraying, cycle.Spraying, weatherKnown),
                NextFertilizingPrediction = cycle.Fertilizing is null ? null : BlockCycleService.ToTask(s, ActivityType.FertilizerApplication, cycle.Fertilizing, weatherKnown)
            };
        }).ToList();
    }
}
