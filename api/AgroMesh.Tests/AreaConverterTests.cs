using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Utilities;

namespace AgroMesh.Tests;

public class AreaConverterTests
{
    [Fact]
    public void ToHectares_FromAcres_ConvertsCorrectly()
    {
        var result = AreaConverter.ToHectares(100, AreaUnit.Acres);
        Assert.InRange(result, 40.46, 40.48);
    }

    [Fact]
    public void ToAcres_FromHectares_ConvertsCorrectly()
    {
        var result = AreaConverter.ToAcres(18);
        Assert.Equal(44.48, result);
    }

    [Fact]
    public void ToDisplayUnit_HectaresPreference_ReturnsHectares()
    {
        var result = AreaConverter.ToDisplayUnit(18.0, AreaUnit.Hectares);
        Assert.Equal(18.0, result);
    }

    [Fact]
    public void ToDisplayUnit_AcresPreference_ReturnsAcres()
    {
        var result = AreaConverter.ToDisplayUnit(18.0, AreaUnit.Acres);
        Assert.Equal(44.48, result);
    }
}
