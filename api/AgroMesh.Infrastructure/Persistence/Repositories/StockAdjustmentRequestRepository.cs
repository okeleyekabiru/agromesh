using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class StockAdjustmentRequestRepository : IStockAdjustmentRequestRepository
{
    private readonly AgroMeshDbContext _db;

    public StockAdjustmentRequestRepository(AgroMeshDbContext db) => _db = db;

    public async Task<StockAdjustmentRequest?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.StockAdjustmentRequests.FindAsync([id], ct);

    public async Task<List<StockAdjustmentRequest>> GetByFarmIdAsync(string farmId, StockAdjustmentStatus? status = null, CancellationToken ct = default)
    {
        var query = _db.StockAdjustmentRequests.Where(r => r.FarmId == farmId);
        if (status is not null)
            query = query.Where(r => r.Status == status);
        return await query.OrderByDescending(r => r.CreatedAt).ToListAsync(ct);
    }

    public async Task<StockAdjustmentRequest> AddAsync(StockAdjustmentRequest request, CancellationToken ct = default)
    {
        _db.StockAdjustmentRequests.Add(request);
        await _db.SaveChangesAsync(ct);
        return request;
    }

    public async Task UpdateAsync(StockAdjustmentRequest request, CancellationToken ct = default)
    {
        _db.StockAdjustmentRequests.Update(request);
        await _db.SaveChangesAsync(ct);
    }
}
