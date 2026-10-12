using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Transaction;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Tests;

public class FinanceSummaryTests
{
    [Fact]
    public async Task Capital_is_kept_apart_from_income_and_expenses()
    {
        var now = DateTime.UtcNow;
        Transaction Tx(decimal amt, string n) => Transaction.Create("f1", Guid.NewGuid().ToString(), n, amt, now,
            TransactionCategory.Other, ConfidenceLevel.High, CategorizationSource.Manual);

        var repo = new FinanceFakeRepo([Tx(100000, "capital from owner"), Tx(40000, "palm oil sale"), Tx(-15000, "ORD-1/chemical")]);
        var s = await new GetFinanceSummaryQueryHandler(repo).Handle(new GetFinanceSummaryQuery("f1"), default);

        Assert.Equal(100000, s.Capital);
        Assert.Equal(40000, s.Income);
        Assert.Equal(15000, s.Expenses);
        Assert.Equal(3, s.PendingReview);   // none of the three has been reviewed by a person
    }

    private class FinanceFakeRepo(List<Transaction> rows) : ITransactionRepository
    {
        public Task<List<Transaction>> GetBetweenAsync(string farmId, DateTime from, DateTime to, CancellationToken ct = default) =>
            Task.FromResult(rows.Where(t => t.Date >= from && t.Date < to).ToList());
        public Task<List<Transaction>> GetNeedingReviewAsync(string farmId, CancellationToken ct = default) => Task.FromResult(new List<Transaction>());
        public Task<Transaction?> GetByIdAsync(string id, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Transaction?> GetByMonoIdAsync(string monoId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<(List<Transaction> Items, int Total)> GetByFarmIdAsync(string farmId, int page, int pageSize, bool? pendingReview = null, AgroMesh.Domain.Enums.TransactionKind? kind = null, AgroMesh.Domain.Enums.TransactionCategory? category = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Transaction>> GetUncategorizedAsync(string farmId, int limit = 100, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<Transaction> AddAsync(Transaction transaction, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateAsync(Transaction transaction, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> ExistsByMonoIdAsync(string monoId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<int> GetPendingReviewCountAsync(string farmId, CancellationToken ct = default) => Task.FromResult(rows.Count(t => !t.ReviewedByUser));
        public Task<decimal> GetTotalSpendMtdAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<MonthAmount>> GetMonthlySpendAsync(string farmId, DateTime from, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<List<Transaction>> GetMtdAsync(string farmId, CancellationToken ct = default) => throw new NotImplementedException();
    }
}
