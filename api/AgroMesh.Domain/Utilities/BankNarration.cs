using System.Text.RegularExpressions;

namespace AgroMesh.Domain.Utilities;

/// <summary>
/// Reads the free text a bank puts on a transfer. Two things matter to the farm: the order reference the
/// payer typed in (for example "ORD-1042/chemical") and whether money coming in is the owner's own capital.
/// </summary>
public static partial class BankNarration
{
    [GeneratedRegex(@"\bORD[\s\-_]*0*(\d{1,9})\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex OrderRef();

    [GeneratedRegex(@"\b(capital|investment)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CapitalWord();

    /// <summary>The first order number named in the narration ("ORD-1042/chemical" gives 1042), or null.</summary>
    public static int? TryGetOrderNumber(string? narration)
    {
        if (string.IsNullOrWhiteSpace(narration)) return null;
        var m = OrderRef().Match(narration);
        return m.Success && int.TryParse(m.Groups[1].Value, out var n) && n > 0 ? n : null;
    }

    /// <summary>True when the narration says the money is capital or an investment by the owner.</summary>
    public static bool IsCapital(string? narration) =>
        !string.IsNullOrWhiteSpace(narration) && CapitalWord().IsMatch(narration);

    public static string OrderReference(int number) => $"ORD-{number}";
}
