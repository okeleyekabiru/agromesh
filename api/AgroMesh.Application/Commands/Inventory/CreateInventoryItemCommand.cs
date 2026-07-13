using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

/// <summary>
/// The unified, non-transaction-linked "stock in" command — reachable from the Add Item form. It
/// either restocks an existing item (via an explicit id, or a case-insensitive name match on the
/// farm) or creates a brand-new one, and always records a StockMovement for the quantity added.
/// CurrentStock only ever changes through InventoryItem.AddStock — no arithmetic is duplicated here.
/// </summary>
public record CreateInventoryItemCommand(string FarmId, CreateInventoryItemRequest Request) : IRequest<InventoryItemDto>;

public class CreateInventoryItemCommandHandler : IRequestHandler<CreateInventoryItemCommand, InventoryItemDto>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IInventoryCategoryRepository _categoryRepo;

    public CreateInventoryItemCommandHandler(IInventoryRepository inventoryRepo, IInventoryCategoryRepository categoryRepo)
    {
        _inventoryRepo = inventoryRepo;
        _categoryRepo = categoryRepo;
    }

    public async Task<InventoryItemDto> Handle(CreateInventoryItemCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var item = await ResolveTargetItemAsync(command.FarmId, req, ct);
        string categoryName;

        if (item is not null)
        {
            if (!string.Equals(item.Unit, req.Unit, StringComparison.OrdinalIgnoreCase))
                throw new DomainException(
                    $"'{item.Name}' is tracked in {item.Unit}, not {req.Unit}. Enter the quantity in {item.Unit}.");

            item.AddStock(req.Quantity);
            await _inventoryRepo.UpdateAsync(item, ct);
            categoryName = item.Category.Name;
        }
        else
        {
            var category = await _categoryRepo.GetByIdAsync(req.CategoryId, ct)
                ?? throw new DomainException($"Category '{req.CategoryId}' not found.");
            if (category.FarmId != command.FarmId)
                throw new DomainException($"Category '{req.CategoryId}' not found.");

            item = Domain.Entities.InventoryItem.Create(
                command.FarmId, req.Name, req.Unit, category.Id, 0, req.ReorderThreshold, req.ChemicalProductId);
            item.AddStock(req.Quantity);
            await _inventoryRepo.AddAsync(item, ct);
            categoryName = category.Name;
        }

        var movement = StockMovement.CreateIn(item.FarmId, item.Id, req.Quantity);
        await _inventoryRepo.AddMovementAsync(movement, ct);

        return new InventoryItemDto(item.Id, item.FarmId, item.Name, item.Unit, item.CategoryId, categoryName,
            item.CurrentStock, item.ReorderThreshold, item.IsLowStock, item.ChemicalProductId, null, null);
    }

    private async Task<Domain.Entities.InventoryItem?> ResolveTargetItemAsync(
        string farmId, CreateInventoryItemRequest req, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(req.ExistingItemId))
        {
            var existing = await _inventoryRepo.GetByIdAsync(req.ExistingItemId, ct)
                ?? throw new DomainException($"Inventory item '{req.ExistingItemId}' not found.");
            // Defence in depth: the item id came from the client, so make sure it actually
            // belongs to the farm in the route rather than trusting it blindly (no cross-farm
            // restocking via a guessed/foreign id).
            if (existing.FarmId != farmId)
                throw new DomainException($"Inventory item '{req.ExistingItemId}' not found.");
            return existing;
        }

        return await _inventoryRepo.FindByNameAsync(farmId, req.Name, ct);
    }
}
