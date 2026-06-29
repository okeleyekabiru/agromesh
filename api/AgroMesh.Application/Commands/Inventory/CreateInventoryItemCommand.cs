using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

public record CreateInventoryItemCommand(string FarmId, CreateInventoryItemRequest Request) : IRequest<InventoryItemDto>;

public class CreateInventoryItemCommandHandler : IRequestHandler<CreateInventoryItemCommand, InventoryItemDto>
{
    private readonly IInventoryRepository _inventoryRepo;

    public CreateInventoryItemCommandHandler(IInventoryRepository inventoryRepo) => _inventoryRepo = inventoryRepo;

    public async Task<InventoryItemDto> Handle(CreateInventoryItemCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var item = Domain.Entities.InventoryItem.Create(
            command.FarmId, req.Name, req.Unit, req.Category, req.CurrentStock, req.ReorderThreshold, req.ChemicalProductId);
        await _inventoryRepo.AddAsync(item, ct);

        return new InventoryItemDto(item.Id, item.FarmId, item.Name, item.Unit, item.Category,
            item.CurrentStock, item.ReorderThreshold, item.IsLowStock, item.ChemicalProductId, null, null);
    }
}
