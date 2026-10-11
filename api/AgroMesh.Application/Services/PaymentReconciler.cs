using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Utilities;

namespace AgroMesh.Application.Services;

/// <summary>Ties a freshly stored bank debit to the order it pays: marks the order paid, or flags the transfer for a person.</summary>
public class PaymentReconciler
{
    private readonly IOrderRepository _orders;
    private readonly ITransactionRepository _transactions;

    public PaymentReconciler(IOrderRepository orders, ITransactionRepository transactions)
    {
        _orders = orders;
        _transactions = transactions;
    }

    /// <summary>Call after the transaction has been saved. Returns true when an order was marked paid.</summary>
    public async Task<bool> ReconcileAsync(Transaction tx, CancellationToken ct = default)
    {
        var number = BankNarration.TryGetOrderNumber(tx.Narration);
        if (number is null || tx.Amount >= 0) return false;

        var order = await _orders.GetByNumberAsync(tx.FarmId, number.Value, ct);
        var result = OrderPaymentMatcher.Decide(tx, order);

        switch (result.Outcome)
        {
            case PaymentMatch.Paid:
                order!.MarkPaid(tx.Id, tx.Date);
                await _orders.UpdateAsync(order, ct);
                tx.MatchOrder(order.Id);
                await _transactions.UpdateAsync(tx, ct);
                return true;

            case PaymentMatch.NeedsReview:
                tx.FlagForReview(result.Note!, order?.Id);
                await _transactions.UpdateAsync(tx, ct);
                return false;

            default:
                return false;
        }
    }
}
