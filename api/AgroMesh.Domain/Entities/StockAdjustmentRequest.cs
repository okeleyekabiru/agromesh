using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

/// <summary>
/// A requested overwrite of an InventoryItem's CurrentStock, sitting in Pending until an admin
/// Approves (applies it via the existing InventoryItem.AdjustStock — no arithmetic duplicated here)
/// or Rejects it (leaves stock untouched). This entity enforces its own invariants, same convention
/// as InventoryItem.AddStock/DeductStock: throws DomainException on invalid transitions rather than
/// leaving that to callers.
/// </summary>
public class StockAdjustmentRequest : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string InventoryItemId { get; private set; } = string.Empty;

    // Absolute new value — same semantics as InventoryItem.AdjustStock's own parameter, not a delta.
    public decimal RequestedQuantity { get; private set; }
    public StockAdjustmentStatus Status { get; private set; }

    // The requester's reason — required, mirrors PickStockCommand's "Purpose is required" pattern.
    public string Notes { get; private set; } = string.Empty;

    public string RequestedByUserId { get; private set; } = string.Empty;
    public string? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNotes { get; private set; }

    private StockAdjustmentRequest() { }

    public static StockAdjustmentRequest Create(
        string farmId, string inventoryItemId, decimal requestedQuantity, string notes, string requestedByUserId)
    {
        if (string.IsNullOrWhiteSpace(notes))
            throw new DomainException("A reason is required for a stock adjustment request.");
        // Same guard AdjustStock itself enforces — checked here too so an obviously-bad request
        // doesn't sit in the approval queue only to fail when an admin finally reviews it.
        if (requestedQuantity < 0)
            throw new DomainException("Stock cannot be adjusted to a negative value.");

        return new StockAdjustmentRequest
        {
            FarmId = farmId,
            InventoryItemId = inventoryItemId,
            RequestedQuantity = requestedQuantity,
            Notes = notes,
            RequestedByUserId = requestedByUserId,
            Status = StockAdjustmentStatus.Pending,
        };
    }

    public void Approve(string reviewedByUserId, string? reviewNotes = null)
    {
        EnsurePending();
        Status = StockAdjustmentStatus.Approved;
        ApplyReview(reviewedByUserId, reviewNotes);
    }

    public void Reject(string reviewedByUserId, string? reviewNotes = null)
    {
        EnsurePending();
        Status = StockAdjustmentStatus.Rejected;
        ApplyReview(reviewedByUserId, reviewNotes);
    }

    private void EnsurePending()
    {
        if (Status != StockAdjustmentStatus.Pending)
            throw new DomainException($"This stock adjustment request has already been {Status.ToString().ToLowerInvariant()}.");
    }

    private void ApplyReview(string reviewedByUserId, string? reviewNotes)
    {
        ReviewedByUserId = reviewedByUserId;
        ReviewNotes = reviewNotes;
        ReviewedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
