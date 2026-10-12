using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

/// <summary>
/// A manager's decision that a cycle of one kind of work on a block is finished even though the marked areas do not
/// add up to the whole block (GPS drift, a corner nobody could walk). From this moment the block counts as fully covered,
/// and the next work starts a new cycle.
/// </summary>
public class CoverageCompletion : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string BlockId { get; private set; } = string.Empty;
    public ActivityType ActivityType { get; private set; }
    public DateTime CompletedOn { get; private set; }
    public string? Reason { get; private set; }
    public string CompletedByUserId { get; private set; } = string.Empty;

    private CoverageCompletion() { }

    public static CoverageCompletion Create(string farmId, string blockId, ActivityType type, string? reason, string userId)
    {
        if (string.IsNullOrWhiteSpace(blockId)) throw new DomainException("A block is required.");
        return new CoverageCompletion
        {
            FarmId = farmId,
            BlockId = blockId,
            ActivityType = type,
            CompletedOn = DateTime.UtcNow,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CompletedByUserId = userId,
        };
    }
}
