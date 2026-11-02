using Ilevora.Domain.Entities;

namespace Ilevora.Application.Interfaces;

public interface IBoundarySubmissionRepository
{
    Task<BoundarySubmission?> GetByOperationIdAsync(string operationId, CancellationToken ct = default);
    Task<BoundarySubmission> AddAsync(BoundarySubmission submission, CancellationToken ct = default);
}
