using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Utilities;

public static class PolygonValidator
{
    private const double EarthRadiusMeters = 6_371_000;
    private const double MaxConsecutivePointDistanceMeters = 5_000;
    private const int MinimumPoints = 4;
    private const double MinAreaToBoundingBoxRatio = 0.15;

    public static void Validate(List<GeoPoint> points, bool confirmImplausibleGeometry = false)
    {
        if (points.Count < MinimumPoints)
            throw new DomainException($"A boundary needs at least {MinimumPoints} points to form a polygon.");

        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            var distance = HaversineDistanceMeters(a, b);
            if (distance > MaxConsecutivePointDistanceMeters && !confirmImplausibleGeometry)
                throw new DomainException(
                    $"Points {i} and {(i + 1) % points.Count} are {distance / 1000:F1}km apart — " +
                    "this looks like GPS drift or a mistaken tap. Confirm to proceed anyway.");
        }
    }

    /// <summary>
    /// Catches self-intersecting/misordered boundaries: if the shoelace-computed area is
    /// drastically smaller than the polygon's own bounding box would suggest, the corners were
    /// most likely captured out of walking order (crossing edges cancel out area instead of
    /// enclosing it) rather than this actually being a legitimate implausibly-thin plot.
    /// </summary>
    public static void ValidateArea(List<GeoPoint> points, double computedHectares, bool confirmImplausibleGeometry = false)
    {
        if (confirmImplausibleGeometry)
            return;

        var boundingBoxHectares = CalculateBoundingBoxHectares(points);
        if (boundingBoxHectares <= 0)
            throw new DomainException(
                "All corners are at (or nearly at) the same location, so no real boundary could be formed — " +
                "check that your GPS points were captured at different spots. Confirm to proceed anyway.");

        var ratio = computedHectares / boundingBoxHectares;
        if (ratio < MinAreaToBoundingBoxRatio)
            throw new DomainException(
                $"This boundary's computed area ({computedHectares:F4} ha) is far smaller than the spread of your " +
                "points suggests — the corners may have been captured out of walking order, producing a " +
                "self-intersecting shape. Confirm to proceed anyway.");
    }

    public static double HaversineDistanceMeters(GeoPoint a, GeoPoint b)
    {
        var lat1 = a.Lat * Math.PI / 180.0;
        var lat2 = b.Lat * Math.PI / 180.0;
        var dLat = (b.Lat - a.Lat) * Math.PI / 180.0;
        var dLng = (b.Lng - a.Lng) * Math.PI / 180.0;

        var sinDLat = Math.Sin(dLat / 2);
        var sinDLng = Math.Sin(dLng / 2);
        var h = sinDLat * sinDLat + Math.Cos(lat1) * Math.Cos(lat2) * sinDLng * sinDLng;
        var c = 2 * Math.Asin(Math.Min(1, Math.Sqrt(h)));
        return EarthRadiusMeters * c;
    }

    private static double CalculateBoundingBoxHectares(List<GeoPoint> points)
    {
        var minLat = points.Min(p => p.Lat);
        var maxLat = points.Max(p => p.Lat);
        var minLng = points.Min(p => p.Lng);
        var maxLng = points.Max(p => p.Lng);

        var width = HaversineDistanceMeters(new GeoPoint(minLat, minLng), new GeoPoint(minLat, maxLng));
        var height = HaversineDistanceMeters(new GeoPoint(minLat, minLng), new GeoPoint(maxLat, minLng));
        return (width * height) / 10_000.0;
    }
}
