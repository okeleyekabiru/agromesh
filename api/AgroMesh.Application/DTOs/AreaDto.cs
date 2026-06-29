using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Utilities;

namespace AgroMesh.Application.DTOs;

public record AreaDto(double Hectares, double Acres, string DisplayValue, string DisplayUnit)
{
    public static AreaDto From(double hectares, AreaUnit preferredUnit)
    {
        var displayVal = AreaConverter.ToDisplayUnit(hectares, preferredUnit);
        return new AreaDto(
            Math.Round(hectares, 2),
            AreaConverter.ToAcres(hectares),
            displayVal.ToString("F2"),
            AreaConverter.UnitLabel(preferredUnit));
    }
}
