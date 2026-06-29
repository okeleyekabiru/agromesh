using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public record CategorizationResult(
    TransactionCategory Category,
    ConfidenceLevel Confidence,
    CategorizationSource Source);

public interface IFinancialCategorizationService
{
    Task<CategorizationResult> CategorizeAsync(string narration, decimal amount, CancellationToken ct = default);
    Task BatchCategorizeAsync(List<Transaction> transactions, CancellationToken ct = default);
}
