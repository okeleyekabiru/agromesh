using AgroMesh.Application.DTOs;
using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Interfaces;

/// <summary>An activity input resolved to a real stock item and a positive quantity.</summary>
public record ResolvedStockInput(InventoryItem Item, decimal Quantity);

/// <summary>
/// Stock consumption for logged activities. Every input must be a real stock item, and using it
/// always reduces stock: first from what was already picked out for that block (stock that is
/// deducted at pick time, so it only counts as "used"), then — for any remainder — straight from
/// the store.
/// </summary>
public interface IInventoryDeductionService
{
    /// <summary>
    /// Resolves and validates inputs WITHOUT changing anything. Throws DomainException if an item
    /// is unknown / belongs to another farm, a quantity isn't positive, or there isn't enough in
    /// total (picked for this block + in the store). Duplicate items are merged.
    /// </summary>
    Task<List<ResolvedStockInput>> ResolveAsync(string farmId, string blockId, List<InputUsedRequest> inputs, CancellationToken ct = default);

    /// <summary>Applies validated inputs and returns low-stock warnings.</summary>
    Task<List<string>> ApplyAsync(string farmId, string blockId, string activityLogId, List<ResolvedStockInput> inputs, CancellationToken ct = default);
}
