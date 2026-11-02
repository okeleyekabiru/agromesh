using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class WorkerRepository : IWorkerRepository
{
    private readonly IlevoraDbContext _db;

    public WorkerRepository(IlevoraDbContext db) => _db = db;

    public async Task<Worker?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Workers.FindAsync([id], ct);

    public async Task<Worker?> GetByIdWithRoleAsync(string id, CancellationToken ct = default) =>
        await _db.Workers.Include(w => w.Role).FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<List<Worker>> GetByFarmIdAsync(string farmId, bool activeOnly = true, CancellationToken ct = default) =>
        await _db.Workers
            .Include(w => w.Role)
            .Where(w => w.FarmId == farmId && (!activeOnly || w.Active))
            .ToListAsync(ct);

    public async Task<Worker> AddAsync(Worker worker, CancellationToken ct = default)
    {
        _db.Workers.Add(worker);
        await _db.SaveChangesAsync(ct);
        return worker;
    }

    public async Task UpdateAsync(Worker worker, CancellationToken ct = default)
    {
        _db.Workers.Update(worker);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> GetCountByRoleAsync(string workerRoleId, CancellationToken ct = default) =>
        await _db.Workers.CountAsync(w => w.WorkerRoleId == workerRoleId && w.Active, ct);
}
