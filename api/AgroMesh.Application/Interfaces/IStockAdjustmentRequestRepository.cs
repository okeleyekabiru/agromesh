using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface IStockAdjustmentRequestRepository
{
    Task<StockAdjustmentRequest?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<StockAdjustmentRequest>> GetByFarmIdAsync(string farmId, StockAdjustmentStatus? status = null, CancellationToken ct = default);
    Task<StockAdjustmentRequest> AddAsync(StockAdjustmentRequest request, CancellationToken ct = default);
    Task UpdateAsync(StockAdjustmentRequest request, CancellationToken ct = default);
}
