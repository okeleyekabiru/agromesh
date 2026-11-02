using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;

namespace Ilevora.Application.Interfaces;

public interface IStockAdjustmentRequestRepository
{
    Task<StockAdjustmentRequest?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<StockAdjustmentRequest>> GetByFarmIdAsync(string farmId, StockAdjustmentStatus? status = null, CancellationToken ct = default);
    Task<StockAdjustmentRequest> AddAsync(StockAdjustmentRequest request, CancellationToken ct = default);
    Task UpdateAsync(StockAdjustmentRequest request, CancellationToken ct = default);
}
