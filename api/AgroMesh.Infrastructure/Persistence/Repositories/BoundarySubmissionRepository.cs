using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class BoundarySubmissionRepository : IBoundarySubmissionRepository
{
    private readonly AgroMeshDbContext _db;

    public BoundarySubmissionRepository(AgroMeshDbContext db) => _db = db;

    public async Task<BoundarySubmission?> GetByOperationIdAsync(string operationId, CancellationToken ct = default) =>
        await _db.BoundarySubmissions.FirstOrDefaultAsync(b => b.OperationId == operationId, ct);

    public async Task<BoundarySubmission> AddAsync(BoundarySubmission submission, CancellationToken ct = default)
    {
        _db.BoundarySubmissions.Add(submission);
        await _db.SaveChangesAsync(ct);
        return submission;
    }
}
