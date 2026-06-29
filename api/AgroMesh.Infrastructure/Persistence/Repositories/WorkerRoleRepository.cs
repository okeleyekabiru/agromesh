using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class WorkerRoleRepository : IWorkerRoleRepository
{
    private readonly AgroMeshDbContext _db;

    public WorkerRoleRepository(AgroMeshDbContext db) => _db = db;

    public async Task<WorkerRole?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.WorkerRoles.FindAsync([id], ct);

    public async Task<List<WorkerRole>> GetByFarmIdAsync(string farmId, CancellationToken ct = default) =>
        await _db.WorkerRoles.Where(r => r.FarmId == farmId).ToListAsync(ct);

    public async Task<WorkerRole> AddAsync(WorkerRole role, CancellationToken ct = default)
    {
        _db.WorkerRoles.Add(role);
        await _db.SaveChangesAsync(ct);
        return role;
    }

    public async Task UpdateAsync(WorkerRole role, CancellationToken ct = default)
    {
        _db.WorkerRoles.Update(role);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<bool> ExistsAsync(string id, CancellationToken ct = default) =>
        await _db.WorkerRoles.AnyAsync(r => r.Id == id, ct);
}
