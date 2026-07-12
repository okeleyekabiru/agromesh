using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class PolygonValidatorTests
{
    private static List<GeoPoint> ValidSquare() =>
    [
        new(7.3775, 3.9470),
        new(7.3775, 3.9560),
        new(7.3865, 3.9560),
        new(7.3865, 3.9470),
    ];

    // Same four corners as ValidSquare, but with the middle two swapped — traces a
    // self-intersecting "bowtie" instead of walking the perimeter in order.
    private static List<GeoPoint> SelfIntersectingBowtie() =>
    [
        new(7.3775, 3.9470),
        new(7.3865, 3.9560),
        new(7.3775, 3.9560),
        new(7.3865, 3.9470),
    ];

    [Fact]
    public void Validate_FourOrMorePoints_DoesNotThrow()
    {
        PolygonValidator.Validate(ValidSquare());
    }

    [Fact]
    public void Validate_FewerThanFourPoints_ThrowsDomainException()
    {
        var points = new List<GeoPoint> { new(7.3775, 3.9470), new(7.3775, 3.9560), new(7.3865, 3.9560) };
        Assert.Throws<DomainException>(() => PolygonValidator.Validate(points));
    }

    [Fact]
    public void Validate_ConsecutivePointsOver5Km_ThrowsWithoutConfirmation()
    {
        var points = new List<GeoPoint>
        {
            new(7.3775, 3.9470),
            new(7.4675, 3.9470), // ~10km north — implausible corner-to-corner jump
            new(7.3865, 3.9560),
            new(7.3775, 3.9560),
        };

        Assert.Throws<DomainException>(() => PolygonValidator.Validate(points));
    }

    [Fact]
    public void Validate_ConsecutivePointsOver5Km_DoesNotThrowWhenConfirmed()
    {
        var points = new List<GeoPoint>
        {
            new(7.3775, 3.9470),
            new(7.4675, 3.9470),
            new(7.3865, 3.9560),
            new(7.3775, 3.9560),
        };

        PolygonValidator.Validate(points, confirmImplausibleGeometry: true);
    }

    [Fact]
    public void HaversineDistanceMeters_KnownDistance_IsAccurate()
    {
        // 1 degree of longitude at the equator is ~111.32km
        var a = new GeoPoint(0, 0);
        var b = new GeoPoint(0, 1);

        var distance = PolygonValidator.HaversineDistanceMeters(a, b);

        Assert.InRange(distance, 111_000, 111_400);
    }

    [Fact]
    public void ValidateArea_NormalRectangle_DoesNotThrow()
    {
        var points = ValidSquare();
        var hectares = GeoPolygonAreaCalculator.CalculateHectares(points);

        PolygonValidator.ValidateArea(points, hectares);
    }

    [Fact]
    public void ValidateArea_SelfIntersectingBowtie_ThrowsWithoutConfirmation()
    {
        var points = SelfIntersectingBowtie();
        var hectares = GeoPolygonAreaCalculator.CalculateHectares(points);

        // The bowtie's crossing edges cancel out via the shoelace formula, so the computed
        // area is far smaller than the same four corners' bounding box would suggest.
        Assert.Throws<DomainException>(() => PolygonValidator.ValidateArea(points, hectares));
    }

    [Fact]
    public void ValidateArea_SelfIntersectingBowtie_DoesNotThrowWhenConfirmed()
    {
        var points = SelfIntersectingBowtie();
        var hectares = GeoPolygonAreaCalculator.CalculateHectares(points);

        PolygonValidator.ValidateArea(points, hectares, confirmImplausibleGeometry: true);
    }

    // Reproduces a desktop browser repeatedly returning the same cached/Wi-Fi-based fix for
    // every corner tap: all four points are identical, so the bounding box collapses to zero
    // area. This must still be flagged rather than silently accepted as a valid 0ha boundary.
    [Fact]
    public void ValidateArea_AllPointsIdentical_ThrowsWithoutConfirmation()
    {
        var points = new List<GeoPoint>
        {
            new(6.63086, 3.38440),
            new(6.63086, 3.38440),
            new(6.63086, 3.38440),
            new(6.63086, 3.38440),
        };
        var hectares = GeoPolygonAreaCalculator.CalculateHectares(points);

        Assert.Throws<DomainException>(() => PolygonValidator.ValidateArea(points, hectares));
    }

    [Fact]
    public void ValidateArea_AllPointsIdentical_DoesNotThrowWhenConfirmed()
    {
        var points = new List<GeoPoint>
        {
            new(6.63086, 3.38440),
            new(6.63086, 3.38440),
            new(6.63086, 3.38440),
            new(6.63086, 3.38440),
        };
        var hectares = GeoPolygonAreaCalculator.CalculateHectares(points);

        PolygonValidator.ValidateArea(points, hectares, confirmImplausibleGeometry: true);
    }
}
