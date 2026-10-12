using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class Transaction : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string MonoId { get; private set; } = string.Empty;
    public string Narration { get; private set; } = string.Empty;
    public decimal Amount { get; private set; }
    public DateTime Date { get; private set; }
    public TransactionCategory Category { get; private set; }
    public Dictionary<string, double> CropAllocation { get; private set; } = [];
    public ConfidenceLevel Confidence { get; private set; }
    public CategorizationSource Source { get; private set; }
    public bool ReviewedByUser { get; private set; }
    public string? ReviewNotes { get; private set; }

    // Money out is an expense. Money in is income, unless the narration says it is the owner's capital.
    public TransactionKind Kind { get; private set; }

    // Set when the narration names a placed order ("ORD-1042/chemical").
    public string? OrderId { get; private set; }
    // The block the spend is for, when the narration names one ("ORD-1042/Alpha/Fertilizer").
    public string? BlockId { get; private set; }
    public ReconciliationStatus Reconciliation { get; private set; }
    public string? ReconciliationNote { get; private set; }

    private Transaction() { }

    public static Transaction Create(
        string farmId,
        string monoId,
        string narration,
        decimal amount,
        DateTime date,
        TransactionCategory category,
        ConfidenceLevel confidence,
        CategorizationSource source,
        Dictionary<string, double>? cropAllocation = null)
    {
        return new Transaction
        {
            FarmId = farmId,
            MonoId = monoId,
            Narration = narration,
            Amount = amount,
            Date = date,
            Category = category,
            Confidence = confidence,
            Source = source,
            CropAllocation = cropAllocation ?? [],
            Kind = amount < 0
                ? TransactionKind.Expense
                : Utilities.BankNarration.IsCapital(narration) ? TransactionKind.Capital : TransactionKind.Income
        };
    }

    /// <summary>
    /// Applies what the payer wrote in the narration: the block the spend is for and its category. The category is
    /// trusted (High confidence), the crop split follows the block's crops, and a problem (an unknown block or type)
    /// is kept as a note for a person.
    /// </summary>
    public void ApplyNarration(string? blockId, TransactionCategory? category, Dictionary<string, double>? cropAllocation, string? problem)
    {
        if (blockId is not null) BlockId = blockId;
        if (category is { } c)
        {
            Category = c;
            Confidence = ConfidenceLevel.High;
            Source = CategorizationSource.Pattern;
        }
        if (cropAllocation is { Count: > 0 } && CropAllocation.Count == 0) CropAllocation = cropAllocation;
        if (problem is not null && Reconciliation == ReconciliationStatus.None)
        {
            Reconciliation = ReconciliationStatus.NeedsReview;
            ReconciliationNote = problem.Length > 500 ? problem[..500] : problem;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// A payment confirmed against a purchase order is the purchase of stock, not a cost yet: the cost is recognised when that
    /// stock is used on a block, through the daily activities. Counting the payment as well would count it twice.
    /// </summary>
    public bool IsStockPurchase => Reconciliation == ReconciliationStatus.Matched && OrderId is not null;

    public void MatchOrder(string orderId)
    {
        OrderId = orderId;
        Reconciliation = ReconciliationStatus.Matched;
        ReconciliationNote = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void FlagForReview(string note, string? orderId = null)
    {
        OrderId = orderId;
        Reconciliation = ReconciliationStatus.NeedsReview;
        ReconciliationNote = note.Length > 500 ? note[..500] : note;
        UpdatedAt = DateTime.UtcNow;
    }

    public void DismissReconciliation()
    {
        Reconciliation = ReconciliationStatus.Dismissed;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>A person corrects whether money in is income or capital. Money out is always an expense.</summary>
    public void SetKind(TransactionKind kind)
    {
        if (Amount < 0 && kind != TransactionKind.Expense)
            throw new Exceptions.DomainException("Money going out is an expense; only money coming in can be income or capital.");
        if (Amount >= 0 && kind == TransactionKind.Expense)
            throw new Exceptions.DomainException("Money coming in cannot be an expense.");
        Kind = kind;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Review(TransactionCategory category, Dictionary<string, double>? cropAllocation, string? notes = null)
    {
        Category = category;
        if (cropAllocation != null)
            CropAllocation = cropAllocation;
        ReviewedByUser = true;
        ReviewNotes = notes;
        Source = CategorizationSource.Manual;
        Confidence = ConfidenceLevel.High;
        UpdatedAt = DateTime.UtcNow;
    }
}
