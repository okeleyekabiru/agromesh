using System.Text.RegularExpressions;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Infrastructure.Services.Financial;

public static partial class PatternMatcher
{
    private static readonly List<(Regex Pattern, TransactionCategory Category)> Patterns =
    [
        (FuelStationPattern(), TransactionCategory.Fuel),
        (SeedPurchasePattern(), TransactionCategory.Seeds),
        (WagesPattern(), TransactionCategory.Labour),
        (AgriInputPattern(), TransactionCategory.Fertilizer),
        (SalesReceiptPattern(), TransactionCategory.Sales),
        (TransportPattern(), TransactionCategory.Transportation),
    ];

    public static (TransactionCategory? Category, ConfidenceLevel Confidence) TryMatch(string narration)
    {
        foreach (var (pattern, category) in Patterns)
        {
            if (pattern.IsMatch(narration))
                return (category, ConfidenceLevel.Medium);
        }
        return (null, ConfidenceLevel.Low);
    }

    [GeneratedRegex(@"\b(shell|total|mobil|conoil|oando)\b", RegexOptions.IgnoreCase)]
    private static partial Regex FuelStationPattern();

    [GeneratedRegex(@"\bseed(s|ling)?\s+(purchase|buy|bought)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SeedPurchasePattern();

    [GeneratedRegex(@"\b(daily\s+wage|casual\s+work(er)?|farm\s+hand)\b", RegexOptions.IgnoreCase)]
    private static partial Regex WagesPattern();

    [GeneratedRegex(@"\b(agro\s+dealer|farm\s+input|agrovet)\b", RegexOptions.IgnoreCase)]
    private static partial Regex AgriInputPattern();

    [GeneratedRegex(@"\b(palm\s+fruit|fresh\s+fruit\s+bunch|ffb|produce\s+sale)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SalesReceiptPattern();

    [GeneratedRegex(@"\b(lorry|pickup|van\s+hire|haulage)\b", RegexOptions.IgnoreCase)]
    private static partial Regex TransportPattern();
}
