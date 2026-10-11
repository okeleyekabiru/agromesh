using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Mapping;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Transaction;

public record SetTransactionKindCommand(string FarmId, string TransactionId, Domain.Enums.TransactionKind Kind)
    : IRequest<TransactionDto>;

/// <summary>Lets a person move a credit between Income and Capital when the narration did not say it.</summary>
public class SetTransactionKindCommandHandler : IRequestHandler<SetTransactionKindCommand, TransactionDto>
{
    private readonly ITransactionRepository _transactions;

    public SetTransactionKindCommandHandler(ITransactionRepository transactions) => _transactions = transactions;

    public async Task<TransactionDto> Handle(SetTransactionKindCommand command, CancellationToken ct)
    {
        var tx = await _transactions.GetByIdAsync(command.TransactionId, ct);
        if (tx is null || tx.FarmId != command.FarmId)
            throw new DomainException($"Transaction '{command.TransactionId}' not found.");

        tx.SetKind(command.Kind);
        await _transactions.UpdateAsync(tx, ct);
        return TransactionMapping.ToDto(tx);
    }
}
