using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Order>> GetByFarmIdAsync(string farmId, OrderStatus? status = null, CancellationToken ct = default);
    Task<Order?> GetByNumberAsync(string farmId, int number, CancellationToken ct = default);
    /// <summary>The next order number for this farm: orders are numbered ORD-1001, ORD-1002 and so on.</summary>
    Task<int> GetNextNumberAsync(string farmId, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
