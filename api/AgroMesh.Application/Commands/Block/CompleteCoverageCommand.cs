using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Block;
using AgroMesh.Domain.Constants;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Block;

/// <summary>
/// Marks a cycle of work on a block as finished by hand, for when the marked areas fall a few metres short of the whole
/// block and would never reach it. The block then counts as fully covered; weeding and spraying get their AI review.
/// </summary>
public record CompleteCoverageCommand(string FarmId, string BlockId, ActivityType ActivityType, string? Reason) : IRequest<BlockCoverageDto>;

public class CompleteCoverageCommandHandler : IRequestHandler<CompleteCoverageCommand, BlockCoverageDto>
{
    private readonly IBlockRepository _blocks;
    private readonly ICoverageCompletionRepository _completions;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IBackgroundJobQueue _jobQueue;
    private readonly IMediator _mediator;

    public CompleteCoverageCommandHandler(
        IBlockRepository blocks, ICoverageCompletionRepository completions, ICurrentUserAccessor currentUser,
        IBackgroundJobQueue jobQueue, IMediator mediator)
    {
        _blocks = blocks;
        _completions = completions;
        _currentUser = currentUser;
        _jobQueue = jobQueue;
        _mediator = mediator;
    }

    public async Task<BlockCoverageDto> Handle(CompleteCoverageCommand c, CancellationToken ct)
    {
        var block = await _blocks.GetByIdAsync(c.BlockId, ct);
        if (block is null || block.FarmId != c.FarmId)
            throw new NotFoundException("Block", c.BlockId);
        if (!ActivityConstants.RequiresWorkedArea.Contains(c.ActivityType))
            throw new DomainException($"{c.ActivityType} does not track a worked area, so there is nothing to complete.");

        var current = await _mediator.Send(new GetBlockCoverageQuery(c.FarmId, c.BlockId, c.ActivityType), ct);
        if (current.Status == "NotStarted")
            throw new DomainException("Nothing has been worked in this cycle yet. Log the work first, then complete it.");
        if (current.Status == "Complete")
            throw new DomainException("This cycle is already complete.");

        var userId = _currentUser.UserId ?? throw new DomainException("Could not identify the user.");
        await _completions.AddAsync(CoverageCompletion.Create(c.FarmId, c.BlockId, c.ActivityType, c.Reason, userId), ct);

        if (c.ActivityType is ActivityType.Weeding or ActivityType.Spraying)
            _jobQueue.EnqueueSchedulingRun(c.FarmId, c.BlockId);

        return await _mediator.Send(new GetBlockCoverageQuery(c.FarmId, c.BlockId, c.ActivityType), ct);
    }
}
