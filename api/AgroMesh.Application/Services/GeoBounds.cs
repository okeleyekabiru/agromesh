using System.Text.Json;

namespace AgroMesh.Application.Services;

public record GeoBox(double MinLat, double MinLng, double MaxLat, double MaxLng)
{
    private const double MetersPerDegreeLat = 111_132.92;
    public double HeightMeters => (MaxLat - MinLat) * MetersPerDegreeLat;
    public double WidthMeters => (MaxLng - MinLng) * MetersPerDegreeLat * Math.Cos((MinLat + MaxLat) / 2 * Math.PI / 180);
}

public static class GeoBounds
{
    /// <summary>The box around every vertex of a Polygon or MultiPolygon, or null when it cannot be read.</summary>
    public static GeoBox? Of(string? geoJson)
    {
        if (string.IsNullOrWhiteSpace(geoJson)) return null;
        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            double minLat = 90, minLng = 180, maxLat = -90, maxLng = -180;
            var any = false;
            void Walk(JsonElement e)
            {
                if (e.ValueKind != JsonValueKind.Array) return;
                if (e.GetArrayLength() >= 2 && e[0].ValueKind == JsonValueKind.Number && e[1].ValueKind == JsonValueKind.Number)
                {
                    var lng = e[0].GetDouble(); var lat = e[1].GetDouble();
                    minLat = Math.Min(minLat, lat); maxLat = Math.Max(maxLat, lat);
                    minLng = Math.Min(minLng, lng); maxLng = Math.Max(maxLng, lng);
                    any = true;
                    return;
                }
                foreach (var child in e.EnumerateArray()) Walk(child);
            }
            Walk(doc.RootElement.GetProperty("coordinates"));
            return any ? new GeoBox(minLat, minLng, maxLat, maxLng) : null;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException or InvalidOperationException)
        {
            return null;
        }
    }
}
