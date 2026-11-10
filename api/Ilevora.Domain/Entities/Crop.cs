using Ilevora.Domain.Exceptions;

namespace Ilevora.Domain.Entities;

/// <summary>
/// A row in the Crops table. Built-in crops (FarmId null, Id = BuiltInCrop.Id) are seeded by the
/// migration; farms add their own (FarmId set, generic fertilizer cycle). Replaces the closed CropType enum.
/// </summary>
public class Crop : BaseEntity
{
    public string? FarmId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsBuiltIn { get; private set; }

    private Crop() { }

    public static Crop CreateCustom(string farmId, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Crop name is required.");
        name = name.Trim();
        if (name.Length > 100)
            throw new DomainException("Crop name must be 100 characters or fewer.");
        return new Crop { FarmId = farmId, Name = name, IsBuiltIn = false };
    }
}
