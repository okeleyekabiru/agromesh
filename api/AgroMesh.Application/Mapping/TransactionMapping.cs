using AgroMesh.Application.DTOs;
using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Mapping;

public static class TransactionMapping
{
    public static TransactionDto ToDto(Transaction t) => new(
        t.Id, t.FarmId, t.MonoId, t.Narration, t.Amount, t.Date, t.Category,
        t.CropAllocation, t.Confidence, t.Source, t.ReviewedByUser, t.ReviewNotes,
        t.Kind, t.OrderId, t.Reconciliation, t.ReconciliationNote);
}
