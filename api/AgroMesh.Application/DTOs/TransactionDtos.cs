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
    string? ReviewNotes);

public record ReviewTransactionRequest(TransactionCategory Category, Dictionary<string, double>? CropAllocation, string? Notes);

public record RecordInventoryReceiptRequest(string InventoryItemId, decimal Quantity);
