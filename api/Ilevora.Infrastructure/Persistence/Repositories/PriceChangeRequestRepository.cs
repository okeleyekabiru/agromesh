using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Ilevora.Infrastructure.Persistence.Repositories;

public class PriceChangeRequestRepository : IPriceChangeRequestRepository
{
    private readonly IlevoraDbContext _db;

    public PriceChangeRequestRepository(IlevoraDbContext db) => _db = db;

    public async Task<PriceChangeRequest?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.PriceChangeRequests.FindAsync([id], ct);

    public async Task<List<PriceChangeRequest>> GetByFarmIdAsync(string farmId, PriceChangeStatus? status = null, CancellationToken ct = default)
    {
        var query = _db.PriceChangeRequests.Where(r => r.FarmId == farmId);
        if (status is not null) query = query.Where(r => r.Status == status);
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
    }

    public async Task<PriceChangeRequest> AddAsync(PriceChangeRequest request, CancellationToken ct = default)
    {
        _db.PriceChangeRequests.Add(request);
        await _db.SaveChangesAsync(ct);
        return request;
    }

    public async Task UpdateAsync(PriceChangeRequest request, CancellationToken ct = default)
    {
        _db.PriceChangeRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }
}
