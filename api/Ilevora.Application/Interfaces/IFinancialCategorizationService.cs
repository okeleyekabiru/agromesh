using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;

namespace Ilevora.Application.Interfaces;

public record CategorizationResult(
    TransactionCategory Category,
    ConfidenceLevel Confidence,
    CategorizationSource Source);

public interface IFinancialCategorizationService
{
    Task<CategorizationResult> CategorizeAsync(string narration, decimal amount, CancellationToken ct = default);
    Task BatchCategorizeAsync(List<Transaction> transactions, CancellationToken ct = default);
}
