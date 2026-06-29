using AgroMesh.Domain.Enums;

namespace AgroMesh.Infrastructure.Services.Financial;

public static class KeywordDictionary
{
    private static readonly Dictionary<string, TransactionCategory> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        // Labour
        ["salary"] = TransactionCategory.Labour,
        ["wages"] = TransactionCategory.Labour,
        ["labour"] = TransactionCategory.Labour,
        ["labor"] = TransactionCategory.Labour,
        ["payroll"] = TransactionCategory.Labour,
        ["worker"] = TransactionCategory.Labour,
        ["staff"] = TransactionCategory.Labour,

        // Fertilizer
        ["fertilizer"] = TransactionCategory.Fertilizer,
        ["fertiliser"] = TransactionCategory.Fertilizer,
        ["npk"] = TransactionCategory.Fertilizer,
        ["urea"] = TransactionCategory.Fertilizer,
        ["compost"] = TransactionCategory.Fertilizer,
        ["manure"] = TransactionCategory.Fertilizer,
        ["agro"] = TransactionCategory.Fertilizer,

        // Chemicals
        ["herbicide"] = TransactionCategory.Chemicals,
        ["pesticide"] = TransactionCategory.Chemicals,
        ["insecticide"] = TransactionCategory.Chemicals,
        ["fungicide"] = TransactionCategory.Chemicals,
        ["glyphosate"] = TransactionCategory.Chemicals,
        ["roundup"] = TransactionCategory.Chemicals,
        ["chemical"] = TransactionCategory.Chemicals,
        ["spray"] = TransactionCategory.Chemicals,

        // Fuel
        ["fuel"] = TransactionCategory.Fuel,
        ["diesel"] = TransactionCategory.Fuel,
        ["petrol"] = TransactionCategory.Fuel,
        ["petroleum"] = TransactionCategory.Fuel,
        ["nafcon"] = TransactionCategory.Fuel,
        ["nnpc"] = TransactionCategory.Fuel,

        // Equipment
        ["equipment"] = TransactionCategory.Equipment,
        ["machinery"] = TransactionCategory.Equipment,
        ["tractor"] = TransactionCategory.Equipment,
        ["chainsaw"] = TransactionCategory.Equipment,
        ["sprayer"] = TransactionCategory.Equipment,
        ["pump"] = TransactionCategory.Equipment,

        // Maintenance
        ["repair"] = TransactionCategory.Maintenance,
        ["maintenance"] = TransactionCategory.Maintenance,
        ["spare"] = TransactionCategory.Maintenance,
        ["service"] = TransactionCategory.Maintenance,

        // Transportation
        ["transport"] = TransactionCategory.Transportation,
        ["truck"] = TransactionCategory.Transportation,
        ["delivery"] = TransactionCategory.Transportation,
        ["haulage"] = TransactionCategory.Transportation,
        ["logistics"] = TransactionCategory.Transportation,

        // Seeds
        ["seed"] = TransactionCategory.Seeds,
        ["seedling"] = TransactionCategory.Seeds,
        ["nursery"] = TransactionCategory.Seeds,

        // Sales
        ["proceeds"] = TransactionCategory.Sales,
        ["harvest"] = TransactionCategory.Sales,
        ["sales"] = TransactionCategory.Sales,
        ["revenue"] = TransactionCategory.Sales,
        ["palm"] = TransactionCategory.Sales,
        ["maize"] = TransactionCategory.Sales,
    };

    public static (TransactionCategory? Category, ConfidenceLevel Confidence) TryMatch(string narration)
    {
        var words = narration.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        foreach (var word in words)
        {
            if (Keywords.TryGetValue(word.Trim(',', '.', '-', '/'), out var cat))
                return (cat, ConfidenceLevel.High);
        }

        return (null, ConfidenceLevel.Low);
    }
}
