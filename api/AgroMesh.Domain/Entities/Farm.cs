using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class Farm : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public double TotalHectares { get; private set; }
    public string OwnerId { get; private set; } = string.Empty;
    public AreaUnit PreferredAreaUnit { get; private set; } = AreaUnit.Hectares;
    public double? CentroidLat { get; private set; }
    public double? CentroidLng { get; private set; }

    public List<Section> Sections { get; private set; } = [];
    public List<WorkerRole> WorkerRoles { get; private set; } = [];
    public List<Worker> Workers { get; private set; } = [];
    public List<CropAllocationRule> AllocationRules { get; private set; } = [];
    public List<InventoryItem> InventoryItems { get; private set; } = [];

    private Farm() { }

    public static Farm Create(string name, string location, double totalHectares, string ownerId, AreaUnit preferredUnit = AreaUnit.Hectares)
    {
        return new Farm
        {
            Name = name,
            Location = location,
            TotalHectares = totalHectares,
            OwnerId = ownerId,
            PreferredAreaUnit = preferredUnit
        };
    }

    public void SetCoordinates(double lat, double lng)
    {
        CentroidLat = lat;
        CentroidLng = lng;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetAreaUnit(AreaUnit unit)
    {
        PreferredAreaUnit = unit;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string name, string location, double totalHectares)
    {
        Name = name;
        Location = location;
        TotalHectares = totalHectares;
        UpdatedAt = DateTime.UtcNow;
    }
}
