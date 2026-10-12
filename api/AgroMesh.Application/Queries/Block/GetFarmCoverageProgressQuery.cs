using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Constants;
using AgroMesh.Domain.Enums;
using MediatR;

namespace AgroMesh.Application.Queries.Block;

/// <summary>Every block with a cycle of work part-done, one row per block and kind of work, so a manager sees what is still open.</summary>
public record GetFarmCoverageProgressQuery(string FarmId) : IRequest<List<BlockCoverageDto>>;

public class GetFarmCoverageProgressQueryHandler : IRequestHandler<GetFarmCoverageProgressQuery, List<BlockCoverageDto>>
{
    private readonly IBlockRepository _blocks;
    private readonly IMediator _mediator;

    public GetFarmCoverageProgressQueryHandler(IBlockRepository blocks, IMediator mediator)
    {
        _blocks = blocks;
        _mediator = mediator;
    }

    public async Task<List<BlockCoverageDto>> Handle(GetFarmCoverageProgressQuery q, CancellationToken ct)
    {
        var result = new List<BlockCoverageDto>();
        // One at a time: the repositories share a single DbContext.
        foreach (var block in (await _blocks.GetByFarmIdAsync(q.FarmId, ct)).Where(b => b.Status == BlockStatus.Active))
            foreach (var type in ActivityConstants.RequiresWorkedArea)
            {
                var c = await _mediator.Send(new GetBlockCoverageQuery(q.FarmId, block.Id, type), ct);
                if (c.Status == "InProgress" && c.HasBoundary) result.Add(c);
            }
        return result.OrderByDescending(c => c.LastWorkedOn).ToList();
    }
}
