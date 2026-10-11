namespace AgroMesh.Domain.Enums;

/// <summary>Whether a bank debit has been tied to a placed order.</summary>
public enum ReconciliationStatus
{
    /// <summary>Nothing to reconcile: the narration names no order.</summary>
    None,
    /// <summary>The narration named an order and the payment matched it, so the order was marked paid.</summary>
    Matched,
    /// <summary>The narration named an order but something did not line up: a person has to look at it.</summary>
    NeedsReview,
    /// <summary>A person looked and decided it is not a payment for an order.</summary>
    Dismissed
}
