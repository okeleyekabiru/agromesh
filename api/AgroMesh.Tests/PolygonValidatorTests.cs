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

    [Fact]
    public void Validate_ThreeOrMorePoints_DoesNotThrow()
    {
        PolygonValidator.Validate(ValidSquare());
    }

    [Fact]
    public void Validate_FewerThanThreePoints_ThrowsDomainException()
    {
        var points = new List<GeoPoint> { new(7.3775, 3.9470), new(7.3775, 3.9560) };
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
        };

        PolygonValidator.Validate(points, confirmImplausibleDistance: true);
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
}
