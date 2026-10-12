using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

/// <summary>
/// A requested price (and pack size) for a stock item, waiting for an admin. The price decides the cost of every activity that uses the
/// item, so it is not applied until an admin approves it. Same convention as StockAdjustmentRequest: it guards its own transitions.
/// </summary>
public class PriceChangeRequest : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string InventoryItemId { get; private set; } = string.Empty;

    public string PackLabel { get; private set; } = "unit";
    public decimal PackSize { get; private set; }
    public decimal PricePerPack { get; private set; }
    // Also fill in the input cost of past activities that used the item and had none.
    public bool BackfillActivities { get; private set; }
    public string? Notes { get; private set; }

    public PriceChangeStatus Status { get; private set; }
    public string RequestedByUserId { get; private set; } = string.Empty;
    public string? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNotes { get; private set; }

    private PriceChangeRequest() { }

    public static PriceChangeRequest Create(
        string farmId, string inventoryItemId, string packLabel, decimal packSize, decimal pricePerPack,
        bool backfillActivities, string? notes, string requestedByUserId)
    {
        if (packSize <= 0) throw new DomainException("Pack size must be greater than zero.");
        if (pricePerPack < 0) throw new DomainException("Price cannot be negative.");

        return new PriceChangeRequest
        {
            FarmId = farmId,
            InventoryItemId = inventoryItemId,
            PackLabel = string.IsNullOrWhiteSpace(packLabel) ? "unit" : packLabel.Trim(),
            PackSize = packSize,
            PricePerPack = pricePerPack,
            BackfillActivities = backfillActivities,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            RequestedByUserId = requestedByUserId,
            Status = PriceChangeStatus.Pending,
        };
    }

    public decimal PricePerUnit => Math.Round(PricePerPack / PackSize, 4);

    public void Approve(string reviewedByUserId, string? reviewNotes = null)
    {
        EnsurePending();
        Status = PriceChangeStatus.Approved;
        ApplyReview(reviewedByUserId, reviewNotes);
    }

    public void Reject(string reviewedByUserId, string? reviewNotes = null)
    {
        EnsurePending();
        Status = PriceChangeStatus.Rejected;
        ApplyReview(reviewedByUserId, reviewNotes);
    }

    private void EnsurePending()
    {
        if (Status != PriceChangeStatus.Pending)
            throw new DomainException($"This price change has already been {Status.ToString().ToLowerInvariant()}.");
    }

    private void ApplyReview(string reviewedByUserId, string? reviewNotes)
    {
        ReviewedByUserId = reviewedByUserId;
        ReviewNotes = reviewNotes;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
