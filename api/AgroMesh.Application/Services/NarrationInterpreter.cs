using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Utilities;

namespace AgroMesh.Application.Services;

/// <summary>
/// Reads "ORD-1042/Alpha/Fertilizer" on a bank transfer and applies it: the spend goes to that block, split across the
/// block's crops, and takes the stated category. The payer spelled it out, so the category is trusted (High confidence).
/// A block name that matches no block is left for a person: the note says so.
/// </summary>
public static class NarrationInterpreter
{
    /// <summary>Returns true when anything was applied. Only money going out is read this way.</summary>
    public static bool Apply(Transaction tx, IReadOnlyList<Block> blocks)
    {
        if (tx.Amount >= 0) return false;
        var parsed = BankNarration.TryParseStructured(tx.Narration);
        if (parsed is null) return false;

        var category = BankNarration.TypeFromText(parsed.Type);
        Block? block = null;
        string? problem = null;

        if (!string.IsNullOrWhiteSpace(parsed.Block))
        {
            var key = BankNarration.BlockKey(parsed.Block);
            block = blocks.FirstOrDefault(b => BankNarration.BlockKey(b.Name) == key);
            if (block is null) problem = $"The narration names block \"{parsed.Block}\", but this farm has no block with that name.";
        }
        if (!string.IsNullOrWhiteSpace(parsed.Type) && category is null)
            problem = (problem is null ? "" : problem + " ") + $"\"{parsed.Type}\" is not a spending type we know (try Fertilizer, Chemicals, Labour, Fuel, Equipment, Maintenance, Transportation or Seeds).";

        if (block is null && category is null && problem is null) return false;

        tx.ApplyNarration(block?.Id, category, AllocationOf(block), problem);
        return true;
    }

    /// <summary>The block's crop mix as percentages, so the cost reaches the right crops.</summary>
    private static Dictionary<string, double>? AllocationOf(Block? block)
    {
        if (block is null || block.Crops.Count == 0) return null;
        return block.Crops
            .GroupBy(c => c.CropType.ToString())
            .ToDictionary(g => g.Key, g => (double)g.Sum(c => c.Percentage));
    }
}
