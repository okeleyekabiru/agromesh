using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface ICoverageCompletionRepository
{
    Task<List<CoverageCompletion>> GetByBlockAsync(string blockId, ActivityType type, CancellationToken ct = default);
    Task AddAsync(CoverageCompletion completion, CancellationToken ct = default);
}
