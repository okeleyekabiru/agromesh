using AgroMesh.Domain.Entities;

namespace AgroMesh.Application.Services;

public enum CropCondition { NoData, Building, Improving, Stable, Declining, DryOrBare }

public record SatelliteSnapshot(CropCondition Condition, string Label, string Summary, DateTime? AsOf, double? Ndvi, double? Ndmi);

/// <summary>
/// Turns a block's recent readings into one plain word. The latest clear reading is compared with the block's own
/// readings from the weeks before, so a block is never judged against another crop or soil.
/// </summary>
public static class SatelliteStatus
{
    public const double ClearEnough = 0.3;        // readings with less clear sky than this are ignored
    public const double Change = 0.08;            // a swing this big in NDVI or NDMI counts as a real change
    public const double BareOrDryNdmi = 0.05;     // vegetation holding this little water is very dry, or the ground is bare
    public const int BaselineFromDays = 15;       // the comparison uses readings this many days ...
    public const int BaselineToDays = 75;         // ... to this many days before the latest one

    public static SatelliteSnapshot Evaluate(IEnumerable<BlockSatelliteReading> readings)
    {
        var usable = readings.Where(r => r.ValidShare >= ClearEnough).OrderBy(r => r.Date).ToList();
        if (usable.Count == 0)
            return new SatelliteSnapshot(CropCondition.NoData, "No clear view yet", "No cloud-free satellite reading for this block yet.", null, null, null);

        var latest = usable[^1];
        var baseline = usable
            .Where(r => (latest.Date - r.Date).TotalDays >= BaselineFromDays && (latest.Date - r.Date).TotalDays <= BaselineToDays)
            .ToList();

        var values = $"NDVI {latest.Ndvi:0.00}, NDMI {latest.Ndmi:0.00}";
        if (latest.Ndmi < BareOrDryNdmi)
            return new SatelliteSnapshot(CropCondition.DryOrBare, "Very dry or bare", $"{values}: the crop is holding very little water, or the ground is bare.", latest.Date, latest.Ndvi, latest.Ndmi);
        if (baseline.Count == 0)
            return new SatelliteSnapshot(CropCondition.Building, "Building history", $"{values}. Not enough earlier readings yet to say whether this is better or worse than usual.", latest.Date, latest.Ndvi, latest.Ndmi);

        var dNdvi = latest.Ndvi - baseline.Average(r => r.Ndvi);
        var dNdmi = latest.Ndmi - baseline.Average(r => r.Ndmi);
        var vs = $"{values} ({Signed(dNdvi)} NDVI, {Signed(dNdmi)} NDMI against the weeks before)";

        if (dNdvi <= -Change || dNdmi <= -Change)
            return new SatelliteSnapshot(CropCondition.Declining, "Declining", $"{vs}: greener cover or leaf water is dropping.", latest.Date, latest.Ndvi, latest.Ndmi);
        if (dNdvi >= Change || dNdmi >= Change)
            return new SatelliteSnapshot(CropCondition.Improving, "Improving", $"{vs}: the crop is greener or holding more water.", latest.Date, latest.Ndvi, latest.Ndmi);
        return new SatelliteSnapshot(CropCondition.Stable, "Stable", $"{vs}: no real change.", latest.Date, latest.Ndvi, latest.Ndmi);
    }

    private static string Signed(double v) => (v >= 0 ? "+" : "") + v.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);
}
