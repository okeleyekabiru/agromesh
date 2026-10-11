using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

/// <summary>
/// Hands leftover stock back to the store after a pick (e.g. 50 kg carried out, 38 kg used, 12 kg
/// returned). Only stock that is still out on the farm for that block can be returned — it settles
/// the oldest picks first and adds the quantity back to the item's stock.
/// </summary>
public record ReturnStockCommand(string FarmId, string InventoryItemId, ReturnStockRequest Request) : IRequest<InventoryItemDto>;

public class ReturnStockCommandHandler : IRequestHandler<ReturnStockCommand, InventoryItemDto>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IBlockRepository _blockRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public ReturnStockCommandHandler(
        IInventoryRepository inventoryRepo, IBlockRepository blockRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _inventoryRepo = inventoryRepo;
        _blockRepo = blockRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<InventoryItemDto> Handle(ReturnStockCommand command, CancellationToken ct)
    {
        var req = command.Request;

        var userId = _currentUser.UserId ?? throw new DomainException("Could not identify the current user.");

        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new NotFoundException("Farm", command.FarmId);
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", command.FarmId);

        if (req.Quantity <= 0) throw new DomainException("Return quantity must be greater than zero.");

        var item = await _inventoryRepo.GetByIdAsync(command.InventoryItemId, ct);
        if (item is null || item.FarmId != command.FarmId)
            throw new DomainException($"Inventory item '{command.InventoryItemId}' not found.");

        var block = await _blockRepo.GetByIdAsync(req.BlockId ?? "", ct);
        if (block is null || block.FarmId != command.FarmId)
            throw new DomainException("Choose the block this leftover stock was picked for.");

        var picks = await _inventoryRepo.GetOutstandingPicksAsync(command.FarmId, item.Id, block.Id, ct);
        var outstanding = picks.Sum(p => p.Outstanding);
        if (req.Quantity > outstanding)
            throw new DomainException(
                $"Only {outstanding} {item.Unit} of '{item.Name}' is still out for {block.Name} — you can't return {req.Quantity} {item.Unit}.");

        var remaining = req.Quantity;
        foreach (var pick in picks)
        {
            if (remaining <= 0) break;
            var take = Math.Min(pick.Outstanding, remaining);
            pick.RecordReturn(take);
            await _inventoryRepo.UpdateMovementAsync(pick, ct);
            remaining -= take;
        }

        item.AddStock(req.Quantity);
        await _inventoryRepo.UpdateAsync(item, ct);
        await _inventoryRepo.AddMovementAsync(
            StockMovement.CreateReturn(command.FarmId, item.Id, req.Quantity, block.Id, userId, req.Notes?.Trim()), ct);

        return new InventoryItemDto(item.Id, item.FarmId, item.Name, item.Unit, item.CategoryId, item.Category.Name,
            item.CurrentStock, item.ReorderThreshold, item.IsLowStock, item.ChemicalProductId, null, DateTime.UtcNow,
            item.PackSize, item.PackLabel, item.UnitCost);
    }
}
