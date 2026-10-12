using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

public interface ITransactionRepository
{
    Task<Transaction?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<Transaction?> GetByMonoIdAsync(string monoId, CancellationToken ct = default);
    Task<(List<Transaction> Items, int Total)> GetByFarmIdAsync(string farmId, int page, int pageSize, bool? pendingReview = null, Domain.Enums.TransactionKind? kind = null, Domain.Enums.TransactionCategory? category = null, CancellationToken ct = default);
    Task<List<Transaction>> GetUncategorizedAsync(string farmId, int limit = 100, CancellationToken ct = default);
    Task<Transaction> AddAsync(Transaction transaction, CancellationToken ct = default);
    Task UpdateAsync(Transaction transaction, CancellationToken ct = default);
    Task<bool> ExistsByMonoIdAsync(string monoId, CancellationToken ct = default);
    Task<int> GetPendingReviewCountAsync(string farmId, CancellationToken ct = default);
    Task<decimal> GetTotalSpendMtdAsync(string farmId, CancellationToken ct = default);
    Task<List<MonthAmount>> GetMonthlySpendAsync(string farmId, DateTime from, CancellationToken ct = default);
    Task<List<Transaction>> GetBetweenAsync(string farmId, DateTime from, DateTime to, CancellationToken ct = default);
    Task<List<Transaction>> GetNeedingReviewAsync(string farmId, CancellationToken ct = default);
    Task<List<Transaction>> GetMtdAsync(string farmId, CancellationToken ct = default);
}
