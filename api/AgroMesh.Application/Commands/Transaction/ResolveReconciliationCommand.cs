using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Transaction;

public record ResolveReconciliationCommand(string FarmId, string TransactionId, ResolveReconciliationRequest Request)
    : IRequest<TransactionDto>;

/// <summary>A person's decision on a transfer that did not match an order cleanly.</summary>
public class ResolveReconciliationCommandHandler : IRequestHandler<ResolveReconciliationCommand, TransactionDto>
{
    private readonly ITransactionRepository _transactions;
    private readonly IOrderRepository _orders;

    public ResolveReconciliationCommandHandler(ITransactionRepository transactions, IOrderRepository orders)
    {
        _transactions = transactions;
        _orders = orders;
    }

    public async Task<TransactionDto> Handle(ResolveReconciliationCommand command, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(command.TransactionId, ct);
        if (tx is null || tx.FarmId != command.FarmId)
            throw new DomainException($"Transaction '{command.TransactionId}' not found.");
        if (tx.Reconciliation != Domain.Enums.ReconciliationStatus.NeedsReview)
            throw new DomainException("This transaction is not waiting for review.");

        if (command.Request.Confirm)
        {
            var orderId = command.Request.OrderId ?? tx.OrderId
                ?? throw new DomainException("Choose the order this payment is for.");
            var order = await _orders.GetByIdAsync(orderId, ct);
            if (order is null || order.FarmId != command.FarmId)
                throw new DomainException($"Order '{orderId}' not found.");

            order.MarkPaid(tx.Id, tx.Date);
            await _orders.UpdateAsync(order, ct);
            tx.MatchOrder(order.Id);
        }
        else
        {
            tx.DismissReconciliation();
        }

        await _transactions.UpdateAsync(tx, ct);
        return TransactionMapping.ToDto(tx);
    }
}
