using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;

namespace AgroMesh.Tests;

public class BlockCycleCalculatorTests
{
    private static readonly DateTime RainyToday = new(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);   // Peak Rains
    private static readonly DateTime DryToday = new(2027, 1, 10, 0, 0, 0, DateTimeKind.Utc);     // Harmattan

    private static CycleInputs Inputs(DateTime today, DateTime? weeded, DateTime? sprayed, double rain30 = 200, double forecast16 = 60,
        IReadOnlyList<DailyWeather>? forecast = null, IReadOnlyList<DailyWeather>? historical = null) =>
        new(today, weeded, sprayed, sprayed is null ? null : "Glyphosate", true, rain30, forecast16, forecast ?? [], historical ?? []);

    [Fact]
    public void No_weeding_is_needed_in_the_dry_season()
    {
        var c = BlockCycleCalculator.Calculate(Inputs(DryToday, DryToday.AddDays(-60), null, rain30: 2, forecast16: 0));
        Assert.Equal(CycleStatus.NotNeeded, c.Weeding.Status);
        Assert.Null(c.Weeding.DueDate);
    }

    [Fact]
    public void A_dry_spell_in_the_rainy_season_also_pauses_weeding()
    {
        var c = BlockCycleCalculator.Calculate(Inputs(RainyToday, RainyToday.AddDays(-30), null, rain30: 10, forecast16: 3));
        Assert.Equal(CycleStatus.NotNeeded, c.Weeding.Status);
    }

    [Fact]
    public void Heavy_rain_brings_weeding_round_in_about_five_weeks()
    {
        var weeded = RainyToday.AddDays(-14);
        var c = BlockCycleCalculator.Calculate(Inputs(RainyToday, weeded, null, rain30: 200, forecast16: 60));
        Assert.Equal(weeded.AddDays(35), c.Weeding.DueDate);
        Assert.Equal(CycleStatus.OnTrack, c.Weeding.Status);
    }

    [Fact]
    public void Moderate_rain_slows_weeds_so_weeding_comes_later()
    {
        var weeded = RainyToday.AddDays(-14);
        var heavy = BlockCycleCalculator.Calculate(Inputs(RainyToday, weeded, null, rain30: 200, forecast16: 60));
        var moderate = BlockCycleCalculator.Calculate(Inputs(RainyToday, weeded, null, rain30: 80, forecast16: 60));
        Assert.True(moderate.Weeding.DueDate > heavy.Weeding.DueDate);
    }

    [Fact]
    public void A_spray_does_not_move_the_weeding_date()
    {
        var weeded = RainyToday.AddDays(-35);          // plain interval makes weeding due today
        var sprayed = RainyToday.AddDays(-3);
        var withSpray = BlockCycleCalculator.Calculate(Inputs(RainyToday, weeded, sprayed, rain30: 80, forecast16: 60));
        var without = BlockCycleCalculator.Calculate(Inputs(RainyToday, weeded, null, rain30: 80, forecast16: 60));
        Assert.Equal(without.Weeding.DueDate, withSpray.Weeding.DueDate);
        Assert.DoesNotContain(withSpray.Weeding.Factors, f => f.Contains("Sprayed"));
    }

    [Fact]
    public void A_block_that_was_only_sprayed_has_no_weeding_date()
    {
        var c = BlockCycleCalculator.Calculate(Inputs(RainyToday, null, RainyToday.AddDays(-10)));
        Assert.Equal(CycleStatus.Unknown, c.Weeding.Status);
        Assert.Null(c.Weeding.DueDate);
    }

    [Fact]
    public void Spraying_is_more_frequent_in_heavy_rain()
    {
        var sprayed = RainyToday.AddDays(-10);
        var wet = BlockCycleCalculator.Calculate(Inputs(RainyToday, null, sprayed, rain30: 200));
        var drier = BlockCycleCalculator.Calculate(Inputs(RainyToday, null, sprayed, rain30: 40));
        Assert.Equal(sprayed.AddDays(28), wet.Spraying.DueDate);
        Assert.Equal(sprayed.AddDays(56), drier.Spraying.DueDate);
    }

    [Fact]
    public void An_overdue_task_is_flagged_and_recommends_the_next_good_spray_day()
    {
        var sprayed = RainyToday.AddDays(-40);
        var weeded = RainyToday.AddDays(-5);   // ring-weeded since the last spray, so the spray is ready
        var forecast = new[]
        {
            new DailyWeather(RainyToday, 12, 28, 22, 10, RainChance: 90),
            new DailyWeather(RainyToday.AddDays(1), 0.2, 30, 23, 8, RainChance: 10),
        };
        var c = BlockCycleCalculator.Calculate(Inputs(RainyToday, weeded, sprayed, rain30: 200, forecast: forecast));
        Assert.Equal(CycleStatus.Overdue, c.Spraying.Status);
        Assert.Equal(RainyToday.AddDays(1), c.Spraying.RecommendedSprayDate);
    }

    [Fact]
    public void A_block_with_no_history_is_unknown()
    {
        var c = BlockCycleCalculator.Calculate(Inputs(RainyToday, null, null));
        Assert.Equal(CycleStatus.Unknown, c.Weeding.Status);
        Assert.Equal(CycleStatus.Unknown, c.Spraying.Status);
    }

    [Fact]
    public void Without_live_weather_the_season_stands_in_for_rainfall()
    {
        var inputs = new CycleInputs(RainyToday, RainyToday.AddDays(-7), null, null, false, 0, 0, [], []);
        var c = BlockCycleCalculator.Calculate(inputs);
        Assert.Equal(RainyToday.AddDays(-7).AddDays(35), c.Weeding.DueDate);   // Peak Rains assumed heavy
        Assert.Equal(AgroMesh.Domain.Enums.ConfidenceLevel.Low, c.Weeding.Confidence);
    }
}

public class WeedingAndSprayingAreIndependentTests
{
    private static readonly DateTime Today = new(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);

    private static CycleInputs Inputs(DateTime? weeded, DateTime? sprayed, double rain30 = 200) =>
        new(Today, weeded, sprayed, null, true, rain30, 60, [], []);

    [Fact]
    public void A_weeding_does_not_move_the_spray_date()
    {
        var sprayed = Today.AddDays(-40);          // spray interval long passed
        var withWeeding = BlockCycleCalculator.Calculate(Inputs(Today.AddDays(-1), sprayed));
        var without = BlockCycleCalculator.Calculate(Inputs(null, sprayed));
        Assert.Equal(sprayed.AddDays(28), withWeeding.Spraying.DueDate);
        Assert.Equal(without.Spraying.DueDate, withWeeding.Spraying.DueDate);
        Assert.False(withWeeding.Spraying.NeedsWeedingFirst);
        Assert.Null(withWeeding.Spraying.EarliestDate);
        Assert.DoesNotContain("ring-weed", withWeeding.Spraying.Headline);
    }

    [Fact]
    public void A_block_that_was_only_weeded_has_no_spray_date()
    {
        var c = BlockCycleCalculator.Calculate(Inputs(Today.AddDays(-1), null));
        Assert.Equal(CycleStatus.Unknown, c.Spraying.Status);
        Assert.Null(c.Spraying.DueDate);
    }
}
