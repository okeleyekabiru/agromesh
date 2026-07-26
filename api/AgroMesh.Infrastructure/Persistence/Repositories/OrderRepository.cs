using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AgroMeshDbContext _db;

    public OrderRepository(AgroMeshDbContext db) => _db = db;

    public async Task<Order?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Orders.FindAsync([id], ct);

    public async Task<List<Order>> GetByFarmIdAsync(string farmId, OrderStatus? status = null, CancellationToken ct = default) =>
        await _db.Orders
            .Where(o => o.FarmId == farmId && (status == null || o.Status == status))
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(ct);

    public async Task<Order> AddAsync(Order order, CancellationToken ct = default)
    {
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);
        return order;
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        _db.Orders.Update(order);
        await _db.SaveChangesAsync(ct);
    }
}
