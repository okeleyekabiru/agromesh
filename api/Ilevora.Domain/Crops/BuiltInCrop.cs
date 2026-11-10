namespace Ilevora.Domain.Crops;

/// <summary>
/// Smart enum for the crops the app ships with. Each carries its own display name, the ordinal the old
/// CropType enum used on the wire (older mobile builds still send/receive it) and its fertilizer
/// profile. Crops a farm adds itself are rows in the Crops table and resolve to <see cref="GenericFertilizer"/>.
/// </summary>
public sealed class BuiltInCrop
{
    /// <summary>Weeks between fertilizer rounds, and why. Starting defaults for Nigerian conditions.</summary>
    public readonly record struct FertilizerPlan(int Weeks, string Note);

    private readonly Func<int?, int, FertilizerPlan?> _fertilizer;

    private BuiltInCrop(string id, string name, int? legacyOrdinal, bool selectable, Func<int?, int, FertilizerPlan?> fertilizer)
    {
        Id = id; Name = name; LegacyOrdinal = legacyOrdinal; IsSelectable = selectable; _fertilizer = fertilizer;
    }

    /// <summary>Stable code. Equals the old CropType enum member name, so stored crop allocations keep matching.</summary>
    public string Id { get; }
    public string Name { get; }
    /// <summary>Ordinal of the old CropType enum (OilPalm 0, Maize 1, Mixed 2, Cocoa 3, Cassava 4); null for crops added later.</summary>
    public int? LegacyOrdinal { get; }
    /// <summary>False for Mixed: it is kept so old data reads back, but it can no longer be chosen.</summary>
    public bool IsSelectable { get; }

    public FertilizerPlan? Fertilizer(int? plantingYear, int thisYear) => _fertilizer(plantingYear, thisYear);

    public static readonly BuiltInCrop OilPalm = new("OilPalm", "Oil palm", 0, true, (py, year) =>
        py is { } y && year - y < 3
            ? new FertilizerPlan(12, "young oil palm (under 3 years) is fed about every 3 months")
            : new FertilizerPlan(26, "mature oil palm is fed about twice a year"));
    public static readonly BuiltInCrop Maize = new("Maize", "Maize", 1, true, (_, _) => new FertilizerPlan(6, "maize is fed in splits, about 6 weeks apart"));
    public static readonly BuiltInCrop Cocoa = new("Cocoa", "Cocoa", 3, true, (_, _) => new FertilizerPlan(17, "cocoa is fed about 3 times a year"));
    public static readonly BuiltInCrop Cassava = new("Cassava", "Cassava", 4, true, (_, _) => new FertilizerPlan(12, "cassava is fed about every 3 months"));
    public static readonly BuiltInCrop Plantain = new("Plantain", "Plantain", null, true, (_, _) => new FertilizerPlan(12, "plantain is fed about every 3 months"));
    public static readonly BuiltInCrop Banana = new("Banana", "Banana", null, true, (_, _) => new FertilizerPlan(8, "banana is fed about every 2 months"));
    /// <summary>Legacy only: says nothing about what grows, so a block cannot be saved with it.</summary>
    public static readonly BuiltInCrop Mixed = new("Mixed", "Mixed", 2, false, (_, _) => null);

    /// <summary>The crops a user can choose, in display order.</summary>
    public static IReadOnlyList<BuiltInCrop> All { get; } = [OilPalm, Maize, Cocoa, Cassava, Plantain, Banana];

    private static readonly BuiltInCrop[] Known = [OilPalm, Maize, Cocoa, Cassava, Plantain, Banana, Mixed];

    public static BuiltInCrop? FromId(string? id) =>
        Known.FirstOrDefault(c => string.Equals(c.Id, id, StringComparison.OrdinalIgnoreCase));

    public static BuiltInCrop? FromLegacyOrdinal(int ordinal) => Known.FirstOrDefault(c => c.LegacyOrdinal == ordinal);

    /// <summary>What a custom crop gets: fed about every 3 months.</summary>
    public static FertilizerPlan GenericFertilizer { get; } = new(12, "a general crop is fed about every 3 months");
}
