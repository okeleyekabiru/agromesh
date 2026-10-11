using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Infrastructure.Services;

public class InventoryDeductionService : IInventoryDeductionService
{
    private readonly IInventoryRepository _repo;

    public InventoryDeductionService(IInventoryRepository repo) => _repo = repo;

    public async Task<List<ResolvedStockInput>> ResolveAsync(
        string farmId, string blockId, List<InputUsedRequest> inputs, CancellationToken ct = default)
    {
        var resolved = new List<ResolvedStockInput>();

        // Same item entered twice is one demand on stock, validated as a whole.
        foreach (var group in (inputs ?? []).GroupBy(i => i.InventoryItemId))
        {
            var item = await _repo.GetByIdAsync(group.Key, ct);
            // The id came from the client — it must be a stock item of THIS farm.
            if (item is null || item.FarmId != farmId)
                throw new DomainException($"Inventory item '{group.Key}' not found.");

            var quantity = group.Sum(i => (decimal)i.Quantity);
            if (quantity <= 0)
                throw new DomainException($"Quantity of '{item.Name}' must be greater than zero.");

            var picked = (await _repo.GetOutstandingPicksAsync(farmId, item.Id, blockId, ct)).Sum(p => p.Outstanding);
            if (quantity > picked + item.CurrentStock)
                throw new DomainException(
                    $"Not enough '{item.Name}': {quantity} {item.Unit} needed, but only {picked} {item.Unit} is picked out " +
                    $"for this block and {item.CurrentStock} {item.Unit} is in the store.");

            resolved.Add(new ResolvedStockInput(item, quantity));
        }

        return resolved;
    }

    public async Task<List<string>> ApplyAsync(
        string farmId, string blockId, string activityLogId, List<ResolvedStockInput> inputs, CancellationToken ct = default)
    {
        var warnings = new List<string>();

        foreach (var (item, quantity) in inputs)
        {
            var remaining = quantity;

            // 1. Use what was already carried out to this block (oldest pick first). Stock for
            //    these was deducted when they were picked, so only the pick's ledger changes.
            foreach (var pick in await _repo.GetOutstandingPicksAsync(farmId, item.Id, blockId, ct))
            {
                if (remaining <= 0) break;
                var take = Math.Min(pick.Outstanding, remaining);
                pick.RecordUsage(take);
                await _repo.UpdateMovementAsync(pick, ct);
                remaining -= take;
            }

            // 2. Anything beyond that comes straight off the shelf.
            if (remaining > 0)
            {
                item.DeductStock(remaining);
                await _repo.UpdateAsync(item, ct);
                await _repo.AddMovementAsync(StockMovement.CreateOut(
                    farmId, item.Id, remaining, activityLogId, $"Activity usage: {remaining} {item.Unit}", blockId: blockId), ct);
            }

            if (item.IsLowStock)
                warnings.Add($"{item.Name} is running low ({item.CurrentStock} {item.Unit} remaining — reorder threshold: {item.ReorderThreshold})");
        }

        return warnings;
    }
}
