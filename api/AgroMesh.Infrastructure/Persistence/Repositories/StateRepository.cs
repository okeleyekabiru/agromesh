using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class StateRepository : IStateRepository
{
    private readonly AgroMeshDbContext _db;

    public StateRepository(AgroMeshDbContext db) => _db = db;

    public async Task<List<State>> GetAllAsync(CancellationToken ct = default) =>
        await _db.States.OrderBy(s => s.Name).ToListAsync(ct);

    public async Task<State?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.States.FindAsync([id], ct);

    public async Task<bool> AnyAsync(CancellationToken ct = default) =>
        await _db.States.AnyAsync(ct);

    public async Task AddRangeAsync(List<State> states, CancellationToken ct = default)
    {
        await _db.States.AddRangeAsync(states, ct);
        await _db.SaveChangesAsync(ct);
    }
}
