using System.Text.Json;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Utilities;

public static class GeoJsonPolygonBuilder
{
    public static string Build(List<GeoPoint> points)
    {
        var ring = points.Select(p => new[] { p.Lng, p.Lat }).ToList();
        if (ring.Count > 0 && (ring[0][0] != ring[^1][0] || ring[0][1] != ring[^1][1]))
            ring.Add(ring[0]);

        return JsonSerializer.Serialize(new { type = "Polygon", coordinates = new[] { ring } });
    }
}
