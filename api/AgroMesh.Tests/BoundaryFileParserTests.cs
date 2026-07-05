using AgroMesh.Application.Services;
using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Tests;

public class BoundaryFileParserTests
{
    private const string ValidGeoJson = """
    {
      "type": "Polygon",
      "coordinates": [[[3.9470,7.3775],[3.9560,7.3775],[3.9560,7.3865],[3.9470,7.3865],[3.9470,7.3775]]]
    }
    """;

    private const string ValidGeoJsonFeature = """
    {
      "type": "Feature",
      "geometry": { "type": "Polygon", "coordinates": [[[3.9470,7.3775],[3.9560,7.3775],[3.9560,7.3865],[3.9470,7.3865]]] }
    }
    """;

    private const string ValidKml = """
    <?xml version="1.0" encoding="UTF-8"?>
    <kml xmlns="http://www.opengis.net/kml/2.2">
      <Document>
        <Placemark>
          <Polygon>
            <outerBoundaryIs>
              <LinearRing>
                <coordinates>3.9470,7.3775,0 3.9560,7.3775,0 3.9560,7.3865,0 3.9470,7.3865,0 3.9470,7.3775,0</coordinates>
              </LinearRing>
            </outerBoundaryIs>
          </Polygon>
        </Placemark>
      </Document>
    </kml>
    """;

    [Fact]
    public void ParseGeoJson_ValidBarePolygon_ReturnsPointsWithoutClosingDuplicate()
    {
        var points = BoundaryFileParser.ParseGeoJson(ValidGeoJson);

        Assert.Equal(4, points.Count);
        Assert.Equal(7.3775, points[0].Lat);
        Assert.Equal(3.9470, points[0].Lng);
    }

    [Fact]
    public void ParseGeoJson_ValidFeatureWrappedPolygon_ReturnsPoints()
    {
        var points = BoundaryFileParser.ParseGeoJson(ValidGeoJsonFeature);

        Assert.Equal(4, points.Count);
    }

    [Fact]
    public void ParseGeoJson_MalformedJson_ThrowsInvalidBoundaryFileException()
    {
        Assert.Throws<InvalidBoundaryFileException>(() => BoundaryFileParser.ParseGeoJson("{not valid json"));
    }

    [Fact]
    public void ParseGeoJson_NonPolygonGeometry_ThrowsInvalidBoundaryFileException()
    {
        const string pointGeoJson = """{"type":"Point","coordinates":[3.947,7.3775]}""";

        Assert.Throws<InvalidBoundaryFileException>(() => BoundaryFileParser.ParseGeoJson(pointGeoJson));
    }

    [Fact]
    public void ParseKml_ValidPolygon_ReturnsPointsWithoutClosingDuplicate()
    {
        var points = BoundaryFileParser.ParseKml(ValidKml);

        Assert.Equal(4, points.Count);
        Assert.Equal(7.3775, points[0].Lat);
        Assert.Equal(3.9470, points[0].Lng);
    }

    [Fact]
    public void ParseKml_MissingPolygon_ThrowsInvalidBoundaryFileException()
    {
        const string kml = "<kml><Document><Placemark><Point/></Placemark></Document></kml>";

        Assert.Throws<InvalidBoundaryFileException>(() => BoundaryFileParser.ParseKml(kml));
    }

    [Fact]
    public void ParseKml_MalformedXml_ThrowsInvalidBoundaryFileException()
    {
        Assert.Throws<InvalidBoundaryFileException>(() => BoundaryFileParser.ParseKml("<kml><Document>"));
    }
}
