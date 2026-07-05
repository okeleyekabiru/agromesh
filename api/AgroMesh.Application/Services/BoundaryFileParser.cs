using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Application.Services;

public static class BoundaryFileParser
{
    public static List<GeoPoint> ParseGeoJson(string content)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(content);
        }
        catch (JsonException ex)
        {
            throw new InvalidBoundaryFileException($"Could not parse GeoJSON: {ex.Message}");
        }

        var root = doc.RootElement;
        var geometry = root;

        if (root.TryGetProperty("type", out var typeProp))
        {
            var type = typeProp.GetString();
            if (type == "FeatureCollection")
            {
                if (!root.TryGetProperty("features", out var features) || features.GetArrayLength() == 0)
                    throw new InvalidBoundaryFileException("GeoJSON FeatureCollection has no features.");
                geometry = features[0].GetProperty("geometry");
            }
            else if (type == "Feature")
            {
                geometry = root.GetProperty("geometry");
            }
        }

        if (!geometry.TryGetProperty("type", out var geomType) || geomType.GetString() != "Polygon")
            throw new InvalidBoundaryFileException("Only GeoJSON Polygon geometries are supported.");

        if (!geometry.TryGetProperty("coordinates", out var coordinates) || coordinates.GetArrayLength() == 0)
            throw new InvalidBoundaryFileException("GeoJSON Polygon has no coordinates.");

        var outerRing = coordinates[0];
        var points = new List<GeoPoint>();
        foreach (var coord in outerRing.EnumerateArray())
        {
            if (coord.GetArrayLength() < 2)
                throw new InvalidBoundaryFileException("GeoJSON coordinate pair is malformed.");

            var lng = coord[0].GetDouble();
            var lat = coord[1].GetDouble();
            points.Add(new GeoPoint(lat, lng));
        }

        return DropClosingDuplicate(points);
    }

    public static List<GeoPoint> ParseKml(string content)
    {
        XDocument doc;
        try
        {
            doc = XDocument.Parse(content);
        }
        catch (XmlException ex)
        {
            throw new InvalidBoundaryFileException($"Could not parse KML: {ex.Message}");
        }

        var coordinatesElement = doc.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Polygon")
            ?.Descendants().FirstOrDefault(e => e.Name.LocalName == "outerBoundaryIs")
            ?.Descendants().FirstOrDefault(e => e.Name.LocalName == "LinearRing")
            ?.Descendants().FirstOrDefault(e => e.Name.LocalName == "coordinates");

        if (coordinatesElement is null || string.IsNullOrWhiteSpace(coordinatesElement.Value))
            throw new InvalidBoundaryFileException(
                "KML file has no Polygon boundary (Placemark > Polygon > outerBoundaryIs > LinearRing > coordinates).");

        var tuples = coordinatesElement.Value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

        var points = new List<GeoPoint>();
        foreach (var tuple in tuples)
        {
            var parts = tuple.Split(',');
            if (parts.Length < 2 ||
                !double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var lng) ||
                !double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var lat))
                throw new InvalidBoundaryFileException($"KML coordinate '{tuple}' is malformed.");

            points.Add(new GeoPoint(lat, lng));
        }

        return DropClosingDuplicate(points);
    }

    private static List<GeoPoint> DropClosingDuplicate(List<GeoPoint> points)
    {
        if (points.Count > 1 && points[0] == points[^1])
            points.RemoveAt(points.Count - 1);
        return points;
    }
}
