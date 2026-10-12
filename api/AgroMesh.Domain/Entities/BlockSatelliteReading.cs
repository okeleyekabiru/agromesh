namespace AgroMesh.Domain.Entities;

/// <summary>
/// What a satellite saw over one block in one window of days: NDVI (how green and dense the crop is) and NDMI (how much
/// water the leaves hold). Both are indicators of the crop's condition, not a soil-moisture reading.
/// </summary>
public class BlockSatelliteReading : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string BlockId { get; private set; } = string.Empty;

    /// <summary>First day of the window the numbers cover (UTC date).</summary>
    public DateTime Date { get; private set; }

    public double Ndvi { get; private set; }
    public double Ndmi { get; private set; }

    /// <summary>Share of the block's pixels that were clear enough to use (0 to 1); clouds and shadow are left out.</summary>
    public double ValidShare { get; private set; }

    private BlockSatelliteReading() { }

    public static BlockSatelliteReading Create(string farmId, string blockId, DateTime date, double ndvi, double ndmi, double validShare) => new()
    {
        FarmId = farmId,
        BlockId = blockId,
        Date = DateTime.SpecifyKind(date.Date, DateTimeKind.Utc),
        Ndvi = ndvi,
        Ndmi = ndmi,
        ValidShare = validShare,
    };

    public void Update(double ndvi, double ndmi, double validShare)
    {
        Ndvi = ndvi;
        Ndmi = ndmi;
        ValidShare = validShare;
        UpdatedAt = DateTime.UtcNow;
    }
}
