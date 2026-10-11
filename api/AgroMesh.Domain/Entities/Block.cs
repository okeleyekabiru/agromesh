using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Entities;

public class Block : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public List<BlockCrop> Crops { get; private set; } = [];
    public double Hectares { get; private set; }
    public BlockStatus Status { get; private set; } = BlockStatus.Active;
    public DateTime? LastActivityDate { get; private set; }
    public string? BoundaryGeoJson { get; private set; }
    public BoundarySource? BoundarySource { get; private set; }
    public double? CentroidLat { get; private set; }
    public double? CentroidLng { get; private set; }
    public int? TreeCount { get; private set; }
    // Year the crop was planted: how old the crop is decides how much fertilizer it needs.
    public int? PlantingYear { get; private set; }

    private Block() { }

    public static Block Create(string farmId, string name, List<BlockCrop> crops, double hectares, string? boundaryGeoJson = null, int? treeCount = null, int? plantingYear = null)
    {
        ValidateCrops(crops);
        ValidateTreeCount(treeCount);
        ValidatePlantingYear(plantingYear);
        return new Block
        {
            FarmId = farmId,
            Name = name,
            Crops = crops,
            Hectares = hectares,
            BoundaryGeoJson = boundaryGeoJson,
            TreeCount = treeCount,
            PlantingYear = plantingYear
        };
    }

    public void ApplyBoundary(string geoJson, double hectares, double? centroidLat, double? centroidLng, BoundarySource source)
    {
        BoundaryGeoJson = geoJson;
        Hectares = hectares;
        CentroidLat = centroidLat;
        CentroidLng = centroidLng;
        BoundarySource = source;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>False when the area is zero or absurd, e.g. from a boundary captured with bad GPS points.</summary>
    public bool HasPlausibleArea => Hectares > 0 && Hectares <= 100_000;

    /// <summary>Drops a boundary that cannot be trusted so a manual area can take its place.</summary>
    public void ClearBoundary()
    {
        BoundaryGeoJson = null;
        CentroidLat = null;
        CentroidLng = null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetManualHectares(double hectares)
    {
        Hectares = hectares;
        BoundarySource = AgroMesh.Domain.Enums.BoundarySource.ManualEstimate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCrops(List<BlockCrop> crops)
    {
        ValidateCrops(crops);
        Crops = crops;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Rename(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPlantingYear(int? year)
    {
        ValidatePlantingYear(year);
        PlantingYear = year;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetTreeCount(int? treeCount)
    {
        ValidateTreeCount(treeCount);
        TreeCount = treeCount;
        UpdatedAt = DateTime.UtcNow;
    }

    public void MarkActive(DateTime activityDate)
    {
        Status = BlockStatus.Active;
        // Activities can be logged out of order (a backdated entry): the block's last activity is the newest date, not the last one typed in.
        if (LastActivityDate is null || activityDate > LastActivityDate) LastActivityDate = activityDate;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateCrops(List<BlockCrop> crops)
    {
        if (crops == null || crops.Count == 0)
            throw new DomainException("A block must have at least one crop.");

        // "Mixed" says nothing about what is growing, and fertilizer depends on the crop: spell out each crop.
        if (crops.Any(c => c.CropType == CropType.Mixed))
            throw new DomainException("Say which crops grow in a mixed block (for example oil palm with cassava) and their share of the block.");

        var total = crops.Sum(c => c.Percentage);
        if (total != 100)
            throw new DomainException($"Crop percentages must sum to 100. Current total: {total}.");
    }

    private static void ValidatePlantingYear(int? year)
    {
        if (year is { } y && (y < 1950 || y > DateTime.UtcNow.Year + 1))
            throw new DomainException($"Planting year must be between 1950 and {DateTime.UtcNow.Year + 1}.");
    }

    private static void ValidateTreeCount(int? treeCount)
    {
        if (treeCount < 0)
            throw new DomainException("Tree count cannot be negative.");
    }
}
