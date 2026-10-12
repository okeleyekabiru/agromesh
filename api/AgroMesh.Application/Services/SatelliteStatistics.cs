using System.Text.Json;
using AgroMesh.Application.Interfaces;

namespace AgroMesh.Application.Services;

/// <summary>Reads the Sentinel Hub Statistical API reply into observations.</summary>
public static class SatelliteStatistics
{
    /// <summary>A window with fewer clear pixels than this says nothing about the block (all cloud).</summary>
    public const double MinValidShare = 0.1;

    public static List<SatelliteObservation> Parse(string json)
    {
        var result = new List<SatelliteObservation>();
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array) return result;

        foreach (var row in data.EnumerateArray())
        {
            if (!row.TryGetProperty("interval", out var interval)
                || !TryDate(interval, "from", out var from) || !TryDate(interval, "to", out var to)
                || !row.TryGetProperty("outputs", out var outputs)) continue;

            if (!TryStats(outputs, "ndvi", out var ndvi, out var sampleCount, out var noData)
                || !TryStats(outputs, "ndmi", out var ndmi, out _, out _)) continue;

            var valid = sampleCount > 0 ? 1.0 - (double)noData / sampleCount : 0;
            if (valid < MinValidShare) continue;
            result.Add(new SatelliteObservation(from, to, Math.Round(ndvi, 4), Math.Round(ndmi, 4), Math.Round(valid, 3)));
        }
        return result;
    }

    private static bool TryDate(JsonElement el, string name, out DateTime value)
    {
        value = default;
        return el.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String
            && DateTime.TryParse(p.GetString(), null, System.Globalization.DateTimeStyles.AdjustToUniversal | System.Globalization.DateTimeStyles.AssumeUniversal, out value);
    }

    // The mean is "NaN" (a string) when every pixel in the window was masked out.
    private static bool TryStats(JsonElement outputs, string id, out double mean, out long sampleCount, out long noDataCount)
    {
        mean = 0; sampleCount = 0; noDataCount = 0;
        if (!outputs.TryGetProperty(id, out var o) || !o.TryGetProperty("bands", out var bands)
            || !bands.TryGetProperty("B0", out var b0) || !b0.TryGetProperty("stats", out var stats)) return false;
        if (!stats.TryGetProperty("mean", out var m) || m.ValueKind != JsonValueKind.Number) return false;
        mean = m.GetDouble();
        if (double.IsNaN(mean) || double.IsInfinity(mean)) return false;
        if (stats.TryGetProperty("sampleCount", out var sc) && sc.ValueKind == JsonValueKind.Number) sampleCount = sc.GetInt64();
        if (stats.TryGetProperty("noDataCount", out var nd) && nd.ValueKind == JsonValueKind.Number) noDataCount = nd.GetInt64();
        return true;
    }
}
