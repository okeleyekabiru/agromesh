using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Utilities;

namespace AgroMesh.Tests;

public class PayRateConverterTests
{
    [Fact]
    public void ToDailyEquivalent_Daily_ReturnsRateUnchanged()
    {
        var result = PayRateConverter.ToDailyEquivalent(2200m, PayFrequency.Daily);
        Assert.Equal(2200m, result);
    }

    [Fact]
    public void ToDailyEquivalent_Weekly_DividesBySixWorkDays()
    {
        // A 6-day agricultural work week — ₦21,000/week → ₦3,500/day
        var result = PayRateConverter.ToDailyEquivalent(21000m, PayFrequency.Weekly);
        Assert.Equal(3500m, result);
    }

    [Fact]
    public void ToDailyEquivalent_Monthly_DividesByTwentySixWorkDays()
    {
        // 26 work days/month (6-day week × ~4.33 weeks) — ₦91,000/month → ₦3,500/day
        var result = PayRateConverter.ToDailyEquivalent(91000m, PayFrequency.Monthly);
        Assert.Equal(3500m, result);
    }

    [Fact]
    public void ToDailyEquivalent_MonthlyWithRemainder_RoundsToTwoDecimalPlaces()
    {
        var result = PayRateConverter.ToDailyEquivalent(100000m, PayFrequency.Monthly);
        Assert.Equal(3846.15m, result);
    }
}
