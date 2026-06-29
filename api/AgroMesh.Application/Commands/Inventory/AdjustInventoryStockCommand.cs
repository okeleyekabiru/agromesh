using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

public record AdjustInventoryStockCommand(string InventoryItemId, decimal NewQuantity, string? Notes) : IRequest<Unit>;

public class AdjustInventoryStockCommandHandler : IRequestHandler<AdjustInventoryStockCommand, Unit>
{
    private readonly IInventoryRepository _inventoryRepo;

    public AdjustInventoryStockCommandHandler(IInventoryRepository inventoryRepo) => _inventoryRepo = inventoryRepo;

    public async Task<Unit> Handle(AdjustInventoryStockCommand command, CancellationToken ct)
    {
        var item = await _inventoryRepo.GetByIdAsync(command.InventoryItemId, ct)
            ?? throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");

        var delta = command.NewQuantity - item.CurrentStock;
        item.AdjustStock(command.NewQuantity, command.Notes);
        await _inventoryRepo.UpdateAsync(item, ct);

        var movement = StockMovement.CreateAdjustment(item.FarmId, item.Id, delta, command.Notes);
        await _inventoryRepo.AddMovementAsync(movement, ct);

        return Unit.Value;
    }
}
