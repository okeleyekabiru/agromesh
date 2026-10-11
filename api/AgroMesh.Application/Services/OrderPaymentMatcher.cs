using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Utilities;

namespace AgroMesh.Application.Services;

public enum PaymentMatch { NotAnOrderPayment, Paid, NeedsReview }

public record PaymentMatchResult(PaymentMatch Outcome, string? Note = null);

/// <summary>
/// Decides whether a bank debit pays for a placed order. Pure rules, no I/O. A transfer is only trusted to
/// confirm an order on its own when every check passes; anything else goes to a person.
/// </summary>
public static class OrderPaymentMatcher
{
    private const decimal Tolerance = 0.01m;

    /// <param name="order">The order whose number is in the narration (null when none exists in this farm).</param>
    public static PaymentMatchResult Decide(Transaction tx, Order? order)
    {
        var number = BankNarration.TryGetOrderNumber(tx.Narration);
        if (number is null || tx.Amount >= 0) return new(PaymentMatch.NotAnOrderPayment);

        var reference = BankNarration.OrderReference(number.Value);
        var paid = Math.Abs(tx.Amount);

        if (order is null)
            return new(PaymentMatch.NeedsReview, $"The transfer names {reference}, but there is no such order on this farm.");
        if (order.Status == OrderStatus.Draft)
            return new(PaymentMatch.NeedsReview, $"{reference} has not been placed yet, so a payment for it is unexpected.");
        if (order.IsPaid)
            return new(PaymentMatch.NeedsReview, $"{reference} is already marked paid. This could be a duplicate payment.");
        if (Math.Abs(paid - order.Total) > Tolerance)
            return new(PaymentMatch.NeedsReview, $"{reference} totals {order.Total:N2} but the transfer was {paid:N2}.");

        return new(PaymentMatch.Paid);
    }
}
