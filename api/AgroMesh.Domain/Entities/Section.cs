using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Entities;

public class Section : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public List<SectionCrop> Crops { get; private set; } = [];
    public double Hectares { get; private set; }
    public SectionStatus Status { get; private set; } = SectionStatus.Active;
    public DateTime? LastActivityDate { get; private set; }
    public string? BoundaryGeoJson { get; private set; }
    public BoundarySource? BoundarySource { get; private set; }
    public double? CentroidLat { get; private set; }
    public double? CentroidLng { get; private set; }

    public List<Block> Blocks { get; private set; } = [];

    private Section() { }

    public static Section Create(string farmId, string name, List<SectionCrop> crops, double hectares, string? boundaryGeoJson = null)
    {
        ValidateCrops(crops);
        return new Section
        {
            FarmId = farmId,
            Name = name,
            Crops = crops,
            Hectares = hectares,
            BoundaryGeoJson = boundaryGeoJson
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

    public void SetManualHectares(double hectares)
    {
        Hectares = hectares;
        BoundarySource = AgroMesh.Domain.Enums.BoundarySource.ManualEstimate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCrops(List<SectionCrop> crops)
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

    public void MarkActive(DateTime activityDate)
    {
        Status = SectionStatus.Active;
        LastActivityDate = activityDate;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateCrops(List<SectionCrop> crops)
    {
        if (crops == null || crops.Count == 0)
            throw new DomainException("A section must have at least one crop.");

        var total = crops.Sum(c => c.Percentage);
        if (total != 100)
            throw new DomainException($"Crop percentages must sum to 100. Current total: {total}.");
    }
}
