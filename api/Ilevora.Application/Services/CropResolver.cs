using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Crops;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.ValueObjects;

namespace Ilevora.Application.Services;

/// <summary>Turns the crops a client sends into BlockCrops, and BlockCrops into the wire shape.</summary>
public static class CropResolver
{
    public static async Task<List<BlockCrop>> ResolveAsync(
        ICropRepository repo, string farmId, IEnumerable<BlockCropInput> inputs, double totalHectares, CancellationToken ct)
    {
        var result = new List<BlockCrop>();
        foreach (var c in inputs)
        {
            var (id, name) = await ResolveOneAsync(repo, farmId, c, ct);
            result.Add(new BlockCrop(id, name, Math.Round(totalHectares * (c.Percentage / 100.0), 4), (int)c.Percentage));
        }
        return result;
    }

    private static async Task<(string Id, string Name)> ResolveOneAsync(ICropRepository repo, string farmId, BlockCropInput c, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(c.CropId))
        {
            if (BuiltInCrop.FromId(c.CropId) is { } builtIn) return (builtIn.Id, builtIn.Name);
            var custom = await repo.GetByIdAsync(c.CropId, ct);
            if (custom is null || custom.FarmId != farmId)
                throw new DomainException("That crop does not exist on this farm. Pick a crop from the list or add it first.");
            return (custom.Id, custom.Name);
        }
        if (c.CropType is { } ordinal)
        {
            var legacy = BuiltInCrop.FromLegacyOrdinal(ordinal)
                ?? throw new DomainException("Unknown crop. Pick a crop from the list.");
            return (legacy.Id, legacy.Name);
        }
        throw new DomainException("Each crop needs a crop id.");
    }

    public static int WireOrdinal(string cropId) => BuiltInCrop.FromId(cropId)?.LegacyOrdinal ?? BuiltInCrop.Mixed.LegacyOrdinal!.Value;
}
