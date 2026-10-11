using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using MediatR;

namespace AgroMesh.Application.Queries.Transaction;

/// <summary>Income, capital and expenses for a period (default: this month so far), plus how many transfers need a person's eye.</summary>
public record GetFinanceSummaryQuery(string FarmId, DateTime? From = null, DateTime? To = null) : IRequest<FinanceSummaryDto>;

public class GetFinanceSummaryQueryHandler : IRequestHandler<GetFinanceSummaryQuery, FinanceSummaryDto>
{
    private readonly ITransactionRepository _transactions;

    public GetFinanceSummaryQueryHandler(ITransactionRepository transactions) => _transactions = transactions;

    public async Task<FinanceSummaryDto> Handle(GetFinanceSummaryQuery query, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var from = DateTime.SpecifyKind((query.From ?? new DateTime(now.Year, now.Month, 1)).Date, DateTimeKind.Utc);
        var to = DateTime.SpecifyKind((query.To ?? now.Date).Date.AddDays(1), DateTimeKind.Utc);

        var rows = await _transactions.GetBetweenAsync(query.FarmId, from, to, ct);
        var review = await _transactions.GetNeedingReviewAsync(query.FarmId, ct);

        return new FinanceSummaryDto(
            from, to.AddDays(-1),
            Income: rows.Where(t => t.Kind == TransactionKind.Income).Sum(t => t.Amount),
            Capital: rows.Where(t => t.Kind == TransactionKind.Capital).Sum(t => t.Amount),
            Expenses: rows.Where(t => t.Kind == TransactionKind.Expense).Sum(t => -t.Amount),
            NeedsReview: review.Count);
    }
}
