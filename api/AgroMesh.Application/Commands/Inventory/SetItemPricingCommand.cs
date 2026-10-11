using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Inventory;

/// <summary>
/// Sets how an existing stock item is packed and priced (e.g. a 50 kg bag at 28,000). Optionally
/// fills in the input cost of past activities that used the item but were logged before it had a price;
/// activities that already have an input cost are left alone.
/// </summary>
public record SetItemPricingCommand(string FarmId, string ItemId, SetItemPricingRequest Request) : IRequest<SetItemPricingResponse>;

public class SetItemPricingCommandHandler : IRequestHandler<SetItemPricingCommand, SetItemPricingResponse>
{
    private readonly IInventoryRepository _inventoryRepo;
    private readonly IActivityRepository _activityRepo;

    public SetItemPricingCommandHandler(IInventoryRepository inventoryRepo, IActivityRepository activityRepo)
    {
        _inventoryRepo = inventoryRepo;
        _activityRepo = activityRepo;
    }

    public async Task<SetItemPricingResponse> Handle(SetItemPricingCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var item = await _inventoryRepo.GetByIdAsync(command.ItemId, ct);
        if (item is null || item.FarmId != command.FarmId)
            throw new DomainException($"Inventory item '{command.ItemId}' not found.");

        item.SetPack(req.PackLabel, req.PackSize);
        item.SetUnitCost(Math.Round(req.PricePerPack / req.PackSize, 4));
        await _inventoryRepo.UpdateAsync(item, ct);

        var updated = 0;
        if (req.BackfillActivities)
        {
            var costs = (await _inventoryRepo.GetByFarmIdAsync(command.FarmId, ct)).ToDictionary(i => i.Id, i => i.UnitCost);
            foreach (var activity in await _activityRepo.GetUnpricedUsingItemAsync(command.FarmId, item.Id, ct))
            {
                var cost = activity.Inputs.Sum(i =>
                    i.InventoryItemId is not null && costs.TryGetValue(i.InventoryItemId, out var unit)
                        ? (decimal)i.Quantity * unit
                        : 0m);
                if (cost <= 0) continue;
                activity.SetInputCost(Math.Round(cost, 2));
                await _activityRepo.UpdateAsync(activity, ct);
                updated++;
            }
        }

        var dto = new InventoryItemDto(item.Id, item.FarmId, item.Name, item.Unit, item.CategoryId, item.Category.Name,
            item.CurrentStock, item.ReorderThreshold, item.IsLowStock, item.ChemicalProductId, null, null,
            item.PackSize, item.PackLabel, item.UnitCost);
        return new SetItemPricingResponse(dto, updated);
    }
}
