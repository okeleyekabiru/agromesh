using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services;

public class InventoryDeductionService : IInventoryDeductionService
{
    private readonly IInventoryRepository _repo;
    private readonly ILogger<InventoryDeductionService> _logger;

    public InventoryDeductionService(IInventoryRepository repo, ILogger<InventoryDeductionService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<List<string>> DeductAsync(
        string farmId, string activityLogId, List<InputUsed> inputs, CancellationToken ct = default)
    {
        var warnings = new List<string>();

        foreach (var input in inputs)
        {
            var item = await _repo.FindByNameAsync(farmId, input.Item, ct);
            if (item == null)
            {
                _logger.LogWarning("Inventory item '{Item}' not found for farm {FarmId} — skipping deduction", input.Item, farmId);
                continue;
            }

            try
            {
                item.DeductStock((decimal)input.Quantity);
                await _repo.UpdateAsync(item, ct);

                var movement = StockMovement.CreateOut(farmId, item.Id, (decimal)input.Quantity, activityLogId,
                    $"Activity usage: {input.Quantity} {input.Unit}");
                await _repo.AddMovementAsync(movement, ct);

                if (item.IsLowStock)
                    warnings.Add($"{item.Name} is running low ({item.CurrentStock} {item.Unit} remaining — reorder threshold: {item.ReorderThreshold})");
            }
            catch (DomainException ex)
            {
                _logger.LogWarning("Insufficient stock for '{Item}': {Msg}", input.Item, ex.Message);
                warnings.Add($"Low stock: {ex.Message}");
            }
        }

        return warnings;
    }
}
