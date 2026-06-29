using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Transaction;

public record RecordInventoryReceiptCommand(string TransactionId, string InventoryItemId, decimal Quantity) : IRequest<Unit>;

public class RecordInventoryReceiptCommandHandler : IRequestHandler<RecordInventoryReceiptCommand, Unit>
{
    private readonly ITransactionRepository _transactionRepo;
    private readonly IInventoryRepository _inventoryRepo;

    public RecordInventoryReceiptCommandHandler(ITransactionRepository transactionRepo, IInventoryRepository inventoryRepo)
    {
        _transactionRepo = transactionRepo;
        _inventoryRepo = inventoryRepo;
    }

    public async Task<Unit> Handle(RecordInventoryReceiptCommand command, CancellationToken ct)
    {
        var transaction = await _transactionRepo.GetByIdAsync(command.TransactionId, ct)
            ?? throw new DomainException($"Transaction '{command.TransactionId}' not found.");
        var item = await _inventoryRepo.GetByIdAsync(command.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");

        item.AddStock(command.Quantity, $"Receipt from transaction {transaction.MonoId}");
        await _inventoryRepo.UpdateAsync(item, ct);

        var movement = StockMovement.CreateIn(item.FarmId, item.Id, command.Quantity, transaction.Id,
            $"Linked to transaction: {transaction.Narration}");
        await _inventoryRepo.AddMovementAsync(movement, ct);

        return Unit.Value;
    }
}
