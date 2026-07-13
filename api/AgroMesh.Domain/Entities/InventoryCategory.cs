using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

/// <summary>
/// A farm-scoped inventory category (e.g. Chemical, Fertilizer, Fuel). Replaces the old
/// hardcoded InventoryCategory enum so farms can grow their own category list beyond the
/// seeded defaults.
/// </summary>
public class InventoryCategory : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool IsDefault { get; private set; }

    public List<InventoryItem> Items { get; private set; } = [];

    private InventoryCategory() { }

    public static InventoryCategory Create(string farmId, string name, bool isDefault = false)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Category name is required.");

        return new InventoryCategory
        {
            FarmId = farmId,
            Name = name.Trim(),
            IsDefault = isDefault
        };
    }

    /// <summary>
    /// The 6 categories explicitly requested (Chemical, Fertilizer, Farm Tools, Seed, Fuel,
    /// Equipment) plus "Spare Parts" and "Other" — kept as defaults too so the two legacy
    /// InventoryCategory enum values with no direct counterpart in the requested list
    /// (SpareParts, Other) still have a home for existing items to migrate into.
    /// </summary>
    public static readonly string[] DefaultCategoryNames =
    [
        "Chemical", "Fertilizer", "Farm Tools", "Seed", "Fuel", "Equipment", "Spare Parts", "Other"
    ];

    public static List<InventoryCategory> CreateDefaults(string farmId) =>
        DefaultCategoryNames.Select(name => Create(farmId, name, isDefault: true)).ToList();
}
