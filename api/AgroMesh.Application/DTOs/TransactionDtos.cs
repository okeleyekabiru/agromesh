using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.DTOs;

public record TransactionDto(
    string Id,
    string FarmId,
    string MonoId,
    string Narration,
    decimal Amount,
    DateTime Date,
    TransactionCategory Category,
    Dictionary<string, double> CropAllocation,
    ConfidenceLevel Confidence,
    CategorizationSource Source,
    bool ReviewedByUser,
    string? ReviewNotes,
    TransactionKind Kind = TransactionKind.Expense,
    string? OrderId = null,
    ReconciliationStatus Reconciliation = ReconciliationStatus.None,
    string? ReconciliationNote = null,
    string? BlockId = null);

public record ReviewTransactionRequest(TransactionCategory Category, Dictionary<string, double>? CropAllocation, string? Notes);

/// <summary>Confirm = this transfer paid the order (OrderId overrides the one in the narration); otherwise it is not an order payment.</summary>
public record ResolveReconciliationRequest(bool Confirm, string? OrderId = null);

public record SetTransactionKindRequest(TransactionKind Kind);

/// <summary>Money in and out over a period. Capital is the owner's own investment, so it is kept apart from income.</summary>
public record FinanceSummaryDto(
    DateTime From,
    DateTime To,
    decimal Income,
    decimal Capital,
    decimal Expenses,
    int NeedsReview,
    // Every transaction nobody has reviewed yet, whatever its date: the number the "Pending Review" badges show.
    int PendingReview = 0);

