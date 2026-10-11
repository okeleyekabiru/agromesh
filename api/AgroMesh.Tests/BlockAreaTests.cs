using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class BlockAreaTests
{
    private static Block WithBoundary(double hectares)
    {
        var b = Block.Create("f1", "A", [new BlockCrop(CropType.OilPalm, 0, 100)], 0);
        b.ApplyBoundary("{}", hectares, 1, 1, BoundarySource.GpsCapture);
        return b;
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(4.05, true)]
    [InlineData(9.77e19, false)]
    public void Plausible_area_check(double hectares, bool plausible) =>
        Assert.Equal(plausible, WithBoundary(hectares).HasPlausibleArea);

    [Fact]
    public void Clearing_a_boundary_lets_a_manual_area_replace_it()
    {
        var b = WithBoundary(0);
        b.ClearBoundary();
        b.SetManualHectares(10);
        Assert.Null(b.BoundaryGeoJson);
        Assert.Equal(10, b.Hectares);
        Assert.Equal(BoundarySource.ManualEstimate, b.BoundarySource);
    }
}
