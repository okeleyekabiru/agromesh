using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface IBoundarySubmissionRepository
{
    Task<BoundarySubmission?> GetByOperationIdAsync(string operationId, CancellationToken ct = default);
    Task<BoundarySubmission> AddAsync(BoundarySubmission submission, CancellationToken ct = default);
}
