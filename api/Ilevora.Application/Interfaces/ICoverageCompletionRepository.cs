using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;

namespace Ilevora.Application.Interfaces;

public interface ICoverageCompletionRepository
{
    Task<List<CoverageCompletion>> GetByBlockAsync(string blockId, ActivityType type, CancellationToken ct = default);
    Task AddAsync(CoverageCompletion completion, CancellationToken ct = default);
}
