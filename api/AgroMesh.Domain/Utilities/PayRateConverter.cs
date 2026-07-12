using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Utilities;

public static class PayRateConverter
{
    private const int WorkDaysPerWeek = 6;
    private const int WorkDaysPerMonth = 26; // 6-day work week × ~4.33 weeks/month

    /// <summary>
    /// Converts a pay rate at any frequency to its daily equivalent, so labour cost can
    /// still be prorated by hours worked on a given activity regardless of whether the
    /// worker's role is paid daily, weekly, or monthly.
    /// </summary>
    public static decimal ToDailyEquivalent(decimal rate, PayFrequency frequency) => frequency switch
    {
        PayFrequency.Daily => rate,
        PayFrequency.Weekly => Math.Round(rate / WorkDaysPerWeek, 2),
        PayFrequency.Monthly => Math.Round(rate / WorkDaysPerMonth, 2),
        _ => rate
    };
}
