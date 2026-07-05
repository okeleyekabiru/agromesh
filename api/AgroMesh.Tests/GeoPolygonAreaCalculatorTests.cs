using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class GeoPolygonAreaCalculatorTests
{
    [Fact]
    public void CalculateHectares_AxisAlignedRectangle_MatchesIndependentWidthTimesHeight()
    {
        var a = new GeoPoint(7.3775, 3.9470);
        var b = new GeoPoint(7.3775, 3.9560);
        var c = new GeoPoint(7.3865, 3.9560);
        var d = new GeoPoint(7.3865, 3.9470);

        // Independent cross-check via great-circle (haversine) distance, not the
        // projection+shoelace formula under test — avoids a tautological fixture.
        var width = PolygonValidator.HaversineDistanceMeters(a, b);
        var height = PolygonValidator.HaversineDistanceMeters(a, d);
        var expectedHectares = width * height / 10_000.0;

        var actual = GeoPolygonAreaCalculator.CalculateHectares([a, b, c, d]);

        Assert.InRange(actual, expectedHectares * 0.98, expectedHectares * 1.02);
    }

    [Fact]
    public void CalculateHectares_TrianglePoints_ReturnsPositiveArea()
    {
        List<GeoPoint> points =
        [
            new(7.3775, 3.9470),
            new(7.3775, 3.9560),
            new(7.3865, 3.9515),
        ];

        var actual = GeoPolygonAreaCalculator.CalculateHectares(points);

        Assert.True(actual > 0);
    }

    [Fact]
    public void CalculateHectares_FewerThanThreePoints_ThrowsDomainException()
    {
        List<GeoPoint> points = [new(7.3775, 3.9470), new(7.3775, 3.9560)];

        Assert.Throws<DomainException>(() => GeoPolygonAreaCalculator.CalculateHectares(points));
    }

    [Fact]
    public void CalculateHectares_PointOrderReversed_ReturnsSameMagnitude()
    {
        List<GeoPoint> points =
        [
            new(7.3775, 3.9470),
            new(7.3775, 3.9560),
            new(7.3865, 3.9560),
            new(7.3865, 3.9470),
        ];
        var reversed = points.AsEnumerable().Reverse().ToList();

        var forward = GeoPolygonAreaCalculator.CalculateHectares(points);
        var backward = GeoPolygonAreaCalculator.CalculateHectares(reversed);

        Assert.Equal(forward, backward, 2);
    }
}
