using System.Globalization;
using System.Text.RegularExpressions;

namespace Ilevora.Application.Services;

/// <summary>
/// Parses a money amount typed into a form field. The contract is digits with an optional single "." fraction, nothing else:
/// the framework's own decimal binder is culture-dependent and quietly accepts thousands separators ("1,5" becomes 15),
/// exponents ("1e3") and surrounding spaces, any of which stores a different amount than the person meant.
/// </summary>
public static partial class StrictDecimal
{
    [GeneratedRegex(@"\A[0-9]{1,15}(\.[0-9]{1,6})?\z", RegexOptions.CultureInvariant)]
    private static partial Regex Pattern();

    public static bool TryParse(string? text, out decimal value)
    {
        value = 0m;
        if (text is null || !Pattern().IsMatch(text)) return false;
        return decimal.TryParse(text, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out value);
    }
}
