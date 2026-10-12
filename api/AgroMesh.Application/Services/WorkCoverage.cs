using System.Globalization;
using System.Text;
using System.Text.Json;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;
using NetTopologySuite.Geometries;

namespace AgroMesh.Application.Services;

public enum CoverageStatus { NotStarted, InProgress, Complete }

/// <summary>What has been worked in the current cycle of one activity type on one block, and what is left.</summary>
public record CoverageResult(
    CoverageStatus Status,
    bool HasBoundary,
    double BlockHectares,
    double CoveredHectares,
    double? RemainingHectares,
    double Percent,
    DateTime? CycleStartedOn,
    DateTime? LastWorkedOn,
    int ActivityCount,
    string? CoveredGeoJson,
    string? RemainingGeoJson,
    IReadOnlyList<CoverageSection>? Sections = null);

/// <summary>One job's worked area in the current cycle: when it was done, and the outline cut to the block's edge.</summary>
public record CoverageSection(DateTime Date, string GeoJson, double Hectares);

public record WorkedArea(string GeoJson, double Hectares, bool ClippedToBlock);

/// <summary>
/// Geometry for "what has been done and what is left". Shapes are worked in a flat metric frame around the block
/// (equirectangular, like <c>GeoPolygonAreaCalculator</c>): at farm scale that is accurate far beyond GPS error and
/// lets ordinary planar geometry do the cutting and merging.
/// </summary>
public static class WorkCoverage
{
    /// <summary>A marked area needs at least this many points to describe what was worked.</summary>
    public const int MinPoints = 8;

    /// <summary>A cycle is finished once this share of the block is covered (the last corners are never walked exactly).</summary>
    public const double CompleteAt = 0.95;

    private const double MetersPerDegreeLat = 111_132.92;
    private static readonly GeometryFactory Factory = new();

    // ── frame ──

    private sealed class Frame(double lat0, double lng0)
    {
        private readonly double _mPerLng = MetersPerDegreeLat * Math.Cos(lat0 * Math.PI / 180.0);

        public Coordinate ToXy(double lat, double lng) => new((lng - lng0) * _mPerLng, (lat - lat0) * MetersPerDegreeLat);
        public (double Lat, double Lng) ToLatLng(Coordinate c) => (lat0 + c.Y / MetersPerDegreeLat, lng0 + c.X / _mPerLng);
    }

    // ── building the worked polygon from the picked points ──

    /// <summary>
    /// Turns the picked points into the area worked, cut to the block's edge when the block has one.
    /// Throws a <see cref="DomainException"/> with a plain reason when the points cannot make an area.
    /// </summary>
    public static WorkedArea BuildWorkedArea(IReadOnlyList<GeoPoint> points, string? blockBoundaryGeoJson)
    {
        if (points.Count < MinPoints)
            throw new DomainException($"Mark the area that was worked with at least {MinPoints} points (you gave {points.Count}).");

        foreach (var p in points)
            if (p.Lat is < -90 or > 90 || p.Lng is < -180 or > 180 || double.IsNaN(p.Lat) || double.IsNaN(p.Lng))
                throw new DomainException("One of the points is not a real location.");

        var block = TryParse(blockBoundaryGeoJson, out var blockRing) ? blockRing : null;
        var origin = block is not null ? block.Item1 : points[0];
        var frame = new Frame(origin.Lat, origin.Lng);

        var coords = points.Select(p => frame.ToXy(p.Lat, p.Lng)).ToList();
        coords.Add(coords[0].Copy());
        var worked = Factory.CreatePolygon(coords.ToArray());

        if (worked.Area < 1)
            throw new DomainException("The points are all in the same spot, so they do not mark an area. Spread them around the edge of what was worked.");
        if (!worked.IsValid)
            throw new DomainException("The points cross each other. Mark them in order around the edge of the area, without crossing back over.");

        if (block is null)
            return new WorkedArea(ToGeoJson(worked, frame), HectaresOf(worked), false);

        var blockGeometry = block.Item2.Buffer(0);
        var inside = worked.Intersection(blockGeometry);
        if (inside.IsEmpty || inside.Area < 1)
            throw new DomainException("The area you marked is outside this block. Check that the points are inside the block's boundary.");

        var clipped = inside.Area < worked.Area - 1; // more than a square metre fell outside
        return new WorkedArea(ToGeoJson(inside, frame), HectaresOf(inside), clipped);
    }

    // ── coverage over a cycle ──

    /// <summary>
    /// Merges the activities of one type into the current cycle and reports what is covered and what is left.
    /// An activity with no outline (logged before outlines existed) counts as the whole block. Oldest first or
    /// newest first both work: they are sorted here.
    /// </summary>
    public static CoverageResult Compute(string? blockBoundaryGeoJson, IEnumerable<(DateTime Date, string? GeoJson)> activities)
    {
        var ordered = activities.OrderBy(a => a.Date).ToList();
        var hasBoundary = TryParse(blockBoundaryGeoJson, out var parsed);

        if (!hasBoundary)
        {
            // Without a block edge there is nothing to measure against: say work happened, but not how much is left.
            var last = ordered.Count > 0 ? ordered[^1].Date : (DateTime?)null;
            return new CoverageResult(ordered.Count > 0 ? CoverageStatus.InProgress : CoverageStatus.NotStarted,
                false, 0, 0, null, 0, ordered.Count > 0 ? ordered[0].Date : null, last, ordered.Count, null, null);
        }

        var (origin, blockPolygon) = parsed!;
        var frame = new Frame(origin.Lat, origin.Lng);
        var block = blockPolygon.Buffer(0);
        var blockArea = block.Area;
        var blockHa = blockArea / 10_000.0;

        Geometry? covered = null;
        DateTime? cycleStart = null, lastWorked = null;
        var count = 0;
        var sections = new List<CoverageSection>();

        foreach (var (date, geoJson) in ordered)
        {
            var shape = geoJson is null ? block : ParseGeoJson(geoJson, frame)?.Intersection(block) ?? block;

            // The covered area only resets once the whole block is covered: however long a part-done cycle waits,
            // the next work adds to it. A finished cycle ends here, and this work starts the next one.
            if (covered is not null && covered.Area / blockArea >= CompleteAt)
            {
                covered = null; cycleStart = null; count = 0; sections.Clear();
            }

            covered = covered is null ? shape : covered.Union(shape);
            if (!shape.IsEmpty) sections.Add(new CoverageSection(date, ToGeoJson(shape, frame), Round(shape.Area / 10_000.0)));
            cycleStart ??= date;
            lastWorked = date;
            count++;
        }

        if (covered is null)
            return new CoverageResult(CoverageStatus.NotStarted, true, Round(blockHa), 0, Round(blockHa), 0, null, null, 0, null, ToGeoJson(block, frame));

        covered = covered.Intersection(block);
        var remaining = block.Difference(covered);
        var fraction = Math.Min(1.0, covered.Area / blockArea);
        var status = fraction >= CompleteAt ? CoverageStatus.Complete : CoverageStatus.InProgress;

        return new CoverageResult(
            status, true, Round(blockHa), Round(covered.Area / 10_000.0), Round(remaining.Area / 10_000.0),
            Math.Round(fraction * 100, 1), cycleStart, lastWorked, count,
            covered.IsEmpty ? null : ToGeoJson(covered, frame),
            remaining.IsEmpty || remaining.Area < 1 ? null : ToGeoJson(remaining, frame),
            sections);
    }

    private static double Round(double v) => Math.Round(v, 4);
    private static double HectaresOf(Geometry g) => Round(g.Area / 10_000.0);

    // ── GeoJSON in and out ──

    /// <summary>Reads the outer ring of a Polygon (or the first polygon of a MultiPolygon); the first vertex anchors the frame.</summary>
    private static bool TryParse(string? geoJson, out Tuple<GeoPoint, Geometry>? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(geoJson)) return false;
        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var rings = FirstPolygonRings(doc.RootElement);
            if (rings is null || rings.Count == 0 || rings[0].Count < 3) return false;

            var first = rings[0][0];
            var frame = new Frame(first.Lat, first.Lng);
            var geometry = BuildPolygon(rings, frame);
            if (geometry is null || geometry.IsEmpty || geometry.Area < 1) return false;

            result = Tuple.Create<GeoPoint, Geometry>(first, geometry);
            return true;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException or FormatException)
        {
            return false;
        }
    }

    private static Geometry? ParseGeoJson(string geoJson, Frame frame)
    {
        try
        {
            using var doc = JsonDocument.Parse(geoJson);
            var root = doc.RootElement;
            var type = root.GetProperty("type").GetString();
            var coords = root.GetProperty("coordinates");

            if (type == "Polygon") return BuildPolygon(ReadPolygon(coords), frame);
            if (type == "MultiPolygon")
            {
                var polys = coords.EnumerateArray().Select(p => BuildPolygon(ReadPolygon(p), frame)).Where(p => p is not null).Cast<Geometry>().ToList();
                return polys.Count == 0 ? null : Factory.BuildGeometry(polys);
            }
            return null;
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException or KeyNotFoundException)
        {
            return null;
        }
    }

    private static List<List<GeoPoint>>? FirstPolygonRings(JsonElement root)
    {
        var type = root.GetProperty("type").GetString();
        var coords = root.GetProperty("coordinates");
        if (type == "Polygon") return ReadPolygon(coords);
        if (type == "MultiPolygon")
        {
            foreach (var p in coords.EnumerateArray()) return ReadPolygon(p);
        }
        return null;
    }

    private static List<List<GeoPoint>> ReadPolygon(JsonElement rings) =>
        rings.EnumerateArray()
             .Select(ring => ring.EnumerateArray().Select(pt => new GeoPoint(pt[1].GetDouble(), pt[0].GetDouble())).ToList())
             .ToList();

    private static Polygon? BuildPolygon(List<List<GeoPoint>> rings, Frame frame)
    {
        LinearRing? Ring(List<GeoPoint> pts)
        {
            var c = pts.Select(p => frame.ToXy(p.Lat, p.Lng)).ToList();
            if (c.Count < 3) return null;
            if (!c[0].Equals2D(c[^1])) c.Add(c[0].Copy());
            return c.Count < 4 ? null : Factory.CreateLinearRing(c.ToArray());
        }

        var shell = Ring(rings[0]);
        if (shell is null) return null;
        var holes = rings.Skip(1).Select(Ring).Where(r => r is not null).Cast<LinearRing>().ToArray();
        return Factory.CreatePolygon(shell, holes);
    }

    private static string ToGeoJson(Geometry g, Frame frame)
    {
        var polys = new List<Polygon>();
        Collect(g, polys);

        var sb = new StringBuilder();
        sb.Append(polys.Count == 1 ? "{\"type\":\"Polygon\",\"coordinates\":" : "{\"type\":\"MultiPolygon\",\"coordinates\":[");
        for (var i = 0; i < polys.Count; i++)
        {
            if (i > 0) sb.Append(',');
            AppendPolygon(sb, polys[i], frame);
        }
        sb.Append(polys.Count == 1 ? "}" : "]}");
        return sb.ToString();
    }

    private static void Collect(Geometry g, List<Polygon> into)
    {
        switch (g)
        {
            case Polygon p when !p.IsEmpty: into.Add(p); break;
            case GeometryCollection gc: foreach (var part in gc.Geometries) Collect(part, into); break; // includes MultiPolygon
        }
    }

    private static void AppendPolygon(StringBuilder sb, Polygon p, Frame frame)
    {
        sb.Append('[');
        AppendRing(sb, p.ExteriorRing.Coordinates, frame);
        foreach (var hole in p.InteriorRings) { sb.Append(','); AppendRing(sb, hole.Coordinates, frame); }
        sb.Append(']');
    }

    private static void AppendRing(StringBuilder sb, Coordinate[] ring, Frame frame)
    {
        sb.Append('[');
        for (var i = 0; i < ring.Length; i++)
        {
            if (i > 0) sb.Append(',');
            var (lat, lng) = frame.ToLatLng(ring[i]);
            sb.Append('[').Append(lng.ToString("0.000000", CultureInfo.InvariantCulture)).Append(',')
              .Append(lat.ToString("0.000000", CultureInfo.InvariantCulture)).Append(']');
        }
        sb.Append(']');
    }
}
