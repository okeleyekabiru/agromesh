using Ilevora.Domain.Enums;

namespace Ilevora.Domain.Utilities;

public static class AreaConverter
{
    private const double AcresPerHectare = 2.47105;

    public static double ToHectares(double value, AreaUnit unit) =>
        unit == AreaUnit.Acres ? Math.Round(value / AcresPerHectare, 4) : value;

    public static double ToAcres(double hectares) =>
        Math.Round(hectares * AcresPerHectare, 2);

    public static double ToDisplayUnit(double hectares, AreaUnit unit) =>
        unit == AreaUnit.Acres ? ToAcres(hectares) : Math.Round(hectares, 2);

    public static string UnitLabel(AreaUnit unit) =>
        unit == AreaUnit.Acres ? "ac" : "ha";
}
