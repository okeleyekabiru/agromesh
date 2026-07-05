using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Utilities;

public static class PolygonValidator
{
    private const double EarthRadiusMeters = 6_371_000;
    private const double MaxConsecutivePointDistanceMeters = 5_000;

    public static void Validate(List<GeoPoint> points, bool confirmImplausibleDistance = false)
    {
        if (points.Count < 3)
            throw new DomainException("A boundary needs at least 3 points to form a polygon.");

        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            var distance = HaversineDistanceMeters(a, b);
            if (distance > MaxConsecutivePointDistanceMeters && !confirmImplausibleDistance)
                throw new DomainException(
                    $"Points {i} and {(i + 1) % points.Count} are {distance / 1000:F1}km apart — " +
                    "this looks like GPS drift or a mistaken tap. Confirm to proceed anyway.");
        }
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
}
