using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<List<Order>> GetByFarmIdAsync(string farmId, OrderStatus? status = null, CancellationToken ct = default);
    Task<Order> AddAsync(Order order, CancellationToken ct = default);
    Task UpdateAsync(Order order, CancellationToken ct = default);
}
