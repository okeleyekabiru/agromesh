using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

/// <summary>
/// Adds an item to the farm's catalogue (or updates how an existing one is bought), with NO stock. Stock only comes in through a
/// purchase order that is received: that is what ties the money paid (the order's bank transfer) to the stock and so to the cost
/// of each activity that uses it. A quantity here is refused. The item is found by explicit id or a case-insensitive name match.
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

        if (req.Quantity > 0)
            throw new DomainException(
                "Stock comes in through a purchase order. Add the item here without a quantity, then create an order for it " +
                "on the Orders page and receive it when it arrives.");

        var item = await ResolveTargetItemAsync(command.FarmId, req, ct);
        string categoryName;

        if (item is not null)
        {
            if (!string.Equals(item.Unit, req.Unit, StringComparison.OrdinalIgnoreCase))
                throw new DomainException(
                    $"'{item.Name}' is tracked in {item.Unit}, not {req.Unit}. Enter the quantity in {item.Unit}.");

            if (req.PackSize.HasValue)
                item.SetPack(req.PackLabel ?? item.PackLabel, req.PackSize.Value);
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
            if (req.PackSize.HasValue)
                item.SetPack(req.PackLabel ?? "unit", req.PackSize.Value);
            await _inventoryRepo.AddAsync(item, ct);
            categoryName = category.Name;
        }

        return new InventoryItemDto(item.Id, item.FarmId, item.Name, item.Unit, item.CategoryId, categoryName,
            item.CurrentStock, item.ReorderThreshold, item.IsLowStock, item.ChemicalProductId, null, null,
            item.PackSize, item.PackLabel, item.UnitCost);
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
