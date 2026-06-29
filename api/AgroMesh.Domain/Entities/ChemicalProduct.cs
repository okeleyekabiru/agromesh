using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class ChemicalProduct : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Manufacturer { get; private set; }
    public ChemicalCategory ChemType { get; private set; }
    public int TypicalIntervalDays { get; private set; }
    public bool Active { get; private set; } = true;

    private ChemicalProduct() { }

    public static ChemicalProduct Create(
        string name,
        ChemicalCategory chemType,
        int typicalIntervalDays,
        string? manufacturer = null)
    {
        return new ChemicalProduct
        {
            Name = name,
            Manufacturer = manufacturer,
            ChemType = chemType,
            TypicalIntervalDays = typicalIntervalDays
        };
    }

    public void Deactivate()
    {
        Active = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, ChemicalCategory chemType, int typicalIntervalDays, string? manufacturer)
    {
        Name = name;
        Manufacturer = manufacturer;
        ChemType = chemType;
        TypicalIntervalDays = typicalIntervalDays;
        UpdatedAt = DateTime.UtcNow;
    }
}
