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
            CropAllocation = cropAllocation ?? []
        };
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
