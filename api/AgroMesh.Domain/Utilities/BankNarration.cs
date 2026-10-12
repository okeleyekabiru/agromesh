using System.Text.RegularExpressions;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Utilities;

/// <summary>What a payer typed after the order number: "ORD-1042/Alpha/Fertilizer" names the block and the kind of spend.</summary>
public record StructuredNarration(int OrderNumber, string? Block, string? Type);

/// <summary>
/// Reads the free text a bank puts on a transfer. What matters to the farm: the order reference the payer typed in,
/// the block and kind of spend after it ("ORD-1042/Alpha/Fertilizer"), and whether money coming in is the owner's capital.
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

    /// <summary>
    /// Splits "ORD-1042/Alpha/Fertilizer" into order, block and type. With only two parts ("ORD-1042/chemical", the
    /// older format) the second is the type when it names a kind of spend, otherwise it is taken as the block.
    /// Banks often add their own text around the reference, so the pieces are found relative to the order number.
    /// </summary>
    public static StructuredNarration? TryParseStructured(string? narration)
    {
        var order = TryGetOrderNumber(narration);
        if (order is null) return null;

        var parts = narration!.Split('/', StringSplitOptions.TrimEntries);
        var at = Array.FindIndex(parts, p => OrderRef().IsMatch(p));
        if (at < 0) return new StructuredNarration(order.Value, null, null);

        var rest = parts.Skip(at + 1).Where(p => p.Length > 0).ToList();
        return rest.Count switch
        {
            0 => new StructuredNarration(order.Value, null, null),
            1 => TypeFromText(rest[0]) is not null
                    ? new StructuredNarration(order.Value, null, rest[0])
                    : new StructuredNarration(order.Value, rest[0], null),
            _ => new StructuredNarration(order.Value, rest[0], rest[1]),
        };
    }

    /// <summary>The spending category a word stands for ("fertilizer", "diesel", "wages"...), or null if it is not one we know.</summary>
    public static TransactionCategory? TypeFromText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var t = new string(text.Where(char.IsLetter).ToArray()).ToLowerInvariant();
        return t switch
        {
            "fertilizer" or "fertiliser" or "fertilizers" or "fertilisers" or "npk" or "manure" => TransactionCategory.Fertilizer,
            "chemical" or "chemicals" or "herbicide" or "pesticide" or "agrochemical" or "agrochemicals" or "spray" => TransactionCategory.Chemicals,
            "labour" or "labor" or "wages" or "wage" or "salary" or "salaries" or "workers" => TransactionCategory.Labour,
            "fuel" or "diesel" or "petrol" => TransactionCategory.Fuel,
            "equipment" or "tools" or "machinery" => TransactionCategory.Equipment,
            "maintenance" or "repair" or "repairs" => TransactionCategory.Maintenance,
            "transport" or "transportation" or "haulage" or "logistics" => TransactionCategory.Transportation,
            "seed" or "seeds" or "seedling" or "seedlings" => TransactionCategory.Seeds,
            "sale" or "sales" => TransactionCategory.Sales,
            "tax" or "taxes" or "vat" or "stampduty" or "stamp" or "levy" or "charge" or "charges" or "bankcharge" or "bankcharges" or "commission" => TransactionCategory.TaxAndCharges,
            _ => null,
        };
    }

    /// <summary>Block names are compared ignoring case, spaces and punctuation ("North Block" matches "northblock").</summary>
    public static string BlockKey(string? name) =>
        new string((name ?? "").Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();

    /// <summary>True when the narration says the money is capital or an investment by the owner.</summary>
    public static bool IsCapital(string? narration) =>
        !string.IsNullOrWhiteSpace(narration) && CapitalWord().IsMatch(narration);

    public static string OrderReference(int number) => $"ORD-{number}";
}
