using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Transaction;

public record ReviewTransactionCommand(string TransactionId, ReviewTransactionRequest Request) : IRequest<TransactionDto>;

public class ReviewTransactionCommandHandler : IRequestHandler<ReviewTransactionCommand, TransactionDto>
{
    private readonly ITransactionRepository _transactionRepo;

    public ReviewTransactionCommandHandler(ITransactionRepository transactionRepo) => _transactionRepo = transactionRepo;

    public async Task<TransactionDto> Handle(ReviewTransactionCommand command, CancellationToken ct)
    {
        var transaction = await _transactionRepo.GetByIdAsync(command.TransactionId, ct)
            ?? throw new DomainException($"Transaction '{command.TransactionId}' not found.");

        var req = command.Request;
        transaction.Review(req.Category, req.CropAllocation, req.Notes);
        await _transactionRepo.UpdateAsync(transaction, ct);

        return new TransactionDto(
            transaction.Id, transaction.FarmId, transaction.MonoId, transaction.Narration,
            transaction.Amount, transaction.Date, transaction.Category, transaction.CropAllocation,
            transaction.Confidence, transaction.Source, transaction.ReviewedByUser, transaction.ReviewNotes);
    }
}
