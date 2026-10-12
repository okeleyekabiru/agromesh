using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroMesh.Infrastructure.Persistence.Repositories;

public class TransactionRepository : ITransactionRepository
{
    private readonly AgroMeshDbContext _db;

    public TransactionRepository(AgroMeshDbContext db) => _db = db;

    public async Task<Transaction?> GetByIdAsync(string id, CancellationToken ct = default) =>
        await _db.Transactions.FindAsync([id], ct);

    public async Task<Transaction?> GetByMonoIdAsync(string monoId, CancellationToken ct = default) =>
        await _db.Transactions.FirstOrDefaultAsync(t => t.MonoId == monoId, ct);

    public async Task<(List<Transaction> Items, int Total)> GetByFarmIdAsync(
        string farmId, int page, int pageSize, bool? pendingReview = null, Domain.Enums.TransactionKind? kind = null, Domain.Enums.TransactionCategory? category = null, CancellationToken ct = default)
    {
        var query = _db.Transactions.Where(t => t.FarmId == farmId);
        if (pendingReview.HasValue)
            query = query.Where(t => t.ReviewedByUser == !pendingReview.Value);
        if (kind.HasValue)
            query = query.Where(t => t.Kind == kind.Value);
        if (category.HasValue)
            query = query.Where(t => t.Category == category.Value);

        query = query.OrderByDescending(t => t.Date);
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<List<Transaction>> GetUncategorizedAsync(string farmId, int limit = 100, CancellationToken ct = default) =>
        await _db.Transactions
            .Where(t => t.FarmId == farmId && !t.ReviewedByUser)
            .OrderBy(t => t.Date)
            .Take(limit)
            .ToListAsync(ct);

    public async Task<Transaction> AddAsync(Transaction transaction, CancellationToken ct = default)
    {
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync(ct);
        return transaction;
    }

    public async Task UpdateAsync(Transaction transaction, CancellationToken ct = default)
    {
        _db.Transactions.Update(transaction);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<List<Transaction>> GetNeedingReviewAsync(string farmId, CancellationToken ct = default) =>
        await _db.Transactions
            .Where(t => t.FarmId == farmId && t.Reconciliation == Domain.Enums.ReconciliationStatus.NeedsReview)
            .OrderByDescending(t => t.Date)
            .ToListAsync(ct);

    public async Task<bool> ExistsByMonoIdAsync(string monoId, CancellationToken ct = default) =>
        await _db.Transactions.AnyAsync(t => t.MonoId == monoId, ct);

    public async Task<int> GetPendingReviewCountAsync(string farmId, CancellationToken ct = default) =>
        await _db.Transactions.CountAsync(t => t.FarmId == farmId && !t.ReviewedByUser, ct);

    public async Task<decimal> GetTotalSpendMtdAsync(string farmId, CancellationToken ct = default)
    {
        var start = MonthStart();
        return await _db.Transactions
            // Payments for stock (matched to an order) are not costs: the cost comes through the activities that use the stock.
            .Where(t => t.FarmId == farmId && t.Date >= start && t.Amount < 0 && t.Reconciliation != Domain.Enums.ReconciliationStatus.Matched)
            .SumAsync(t => Math.Abs(t.Amount), ct);
    }

    public async Task<List<MonthAmount>> GetMonthlySpendAsync(string farmId, DateTime from, CancellationToken ct = default) =>
        await _db.Transactions
            .Where(t => t.FarmId == farmId && t.Date >= from && t.Amount < 0 && t.Reconciliation != Domain.Enums.ReconciliationStatus.Matched)
            .GroupBy(t => new { t.Date.Year, t.Date.Month })
            .Select(g => new MonthAmount(g.Key.Year, g.Key.Month, g.Sum(t => -t.Amount)))
            .ToListAsync(ct);

    public async Task<List<Transaction>> GetBetweenAsync(string farmId, DateTime from, DateTime to, CancellationToken ct = default) =>
        await _db.Transactions.Where(t => t.FarmId == farmId && t.Date >= from && t.Date < to).ToListAsync(ct);

    public async Task<List<Transaction>> GetMtdAsync(string farmId, CancellationToken ct = default)
    {
        var start = MonthStart();
        return await _db.Transactions.Where(t => t.FarmId == farmId && t.Date >= start).ToListAsync(ct);
    }

    private static DateTime MonthStart()
    {
        var now = DateTime.UtcNow;
        return new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
    }
}
