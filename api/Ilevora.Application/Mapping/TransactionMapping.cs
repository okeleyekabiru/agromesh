using Ilevora.Application.DTOs;
using Ilevora.Domain.Entities;

namespace Ilevora.Application.Mapping;

public static class TransactionMapping
{
    public static TransactionDto ToDto(Transaction t) => new(
        t.Id, t.FarmId, t.MonoId, t.Narration, t.Amount, t.Date, t.Category,
        t.CropAllocation, t.Confidence, t.Source, t.ReviewedByUser, t.ReviewNotes,
        t.Kind, t.OrderId, t.Reconciliation, t.ReconciliationNote, t.BlockId);
}
