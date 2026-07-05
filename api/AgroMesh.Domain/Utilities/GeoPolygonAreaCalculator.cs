using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Domain.Utilities;

public static class GeoPolygonAreaCalculator
{
    private const double MetersPerDegreeLat = 111_132.92;

    /// <summary>
    /// Projects points to local meters (equirectangular, anchored at the first vertex) then
    /// applies the planar shoelace formula. At farm scale this is more numerically stable
    /// than spherical-excess methods and its error is dwarfed by GPS receiver error.
    /// </summary>
    public static double CalculateHectares(List<GeoPoint> points)
    {
        if (points.Count < 3)
            throw new DomainException("A polygon needs at least 3 points to calculate area.");

        var origin = points[0];
        var metersPerDegreeLng = MetersPerDegreeLat * Math.Cos(origin.Lat * Math.PI / 180.0);

        var xy = points.Select(p => (
            X: (p.Lng - origin.Lng) * metersPerDegreeLng,
            Y: (p.Lat - origin.Lat) * MetersPerDegreeLat)).ToList();

        double sum = 0;
        for (var i = 0; i < xy.Count; i++)
        {
            var (x1, y1) = xy[i];
            var (x2, y2) = xy[(i + 1) % xy.Count];
            sum += x1 * y2 - x2 * y1;
        }

        var squareMeters = Math.Abs(sum) / 2.0;
        return Math.Round(squareMeters / 10_000.0, 4);
    }

    public static (double Lat, double Lng) CalculateCentroid(List<GeoPoint> points)
    {
        if (points.Count == 0)
            throw new DomainException("Cannot calculate a centroid with no points.");

        return (points.Average(p => p.Lat), points.Average(p => p.Lng));
    }
}
