using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class StateRepository : IStateRepository
{
    private readonly IlevoraDbContext _db;

    public StateRepository(IlevoraDbContext db) => _db = db;

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
