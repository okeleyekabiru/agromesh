using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Tests;

public class AiCycleReviewTests
{
    private static readonly DateTime Today = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Parses_a_clean_answer()
    {
        var o = AiCycleReview.Parse("""
            {"weeding":{"dueDate":"2026-10-20","reason":"Heavy rain from Thursday, weed first.","confidence":"High"},
             "spraying":{"dueDate":"2026-10-23","reason":"Dry from Friday.","confidence":"Medium"}}
            """);
        Assert.Equal(new DateTime(2026, 10, 20), o!.Weeding!.DueDate);
        Assert.Equal("Heavy rain from Thursday, weed first.", o.Weeding.Reason);
        Assert.Equal(ConfidenceLevel.High, o.Weeding.Confidence);
        Assert.Equal(new DateTime(2026, 10, 23), o.Spraying!.DueDate);
    }

    [Fact]
    public void Copes_with_code_fences_and_chatter_around_the_json()
    {
        var o = AiCycleReview.Parse("Sure!\n```json\n{\"weeding\":{\"dueDate\":\"2026-10-20\",\"reason\":\"ok\"}}\n```");
        Assert.NotNull(o!.Weeding);
        Assert.Null(o.Spraying);
        Assert.Equal(ConfidenceLevel.Medium, o.Weeding!.Confidence);   // default when missing
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("no json here")]
    [InlineData("{ broken")]
    public void Unusable_answers_give_nothing(string? text) => Assert.Null(AiCycleReview.Parse(text));

    [Fact]
    public void A_bad_date_is_ignored_but_the_rest_survives()
    {
        var o = AiCycleReview.Parse("{\"weeding\":{\"dueDate\":\"not a date\",\"reason\":\"x\"}}");
        Assert.Null(o!.Weeding!.DueDate);
    }

    [Fact]
    public void The_ai_can_only_shift_a_date_by_a_week()
    {
        var baseline = Today.AddDays(20);
        Assert.Equal(baseline.AddDays(7), AiCycleReview.ClampToBaseline(baseline.AddDays(40), baseline, Today));
        Assert.Equal(baseline.AddDays(-7), AiCycleReview.ClampToBaseline(baseline.AddDays(-30), baseline, Today));
        Assert.Equal(baseline.AddDays(3), AiCycleReview.ClampToBaseline(baseline.AddDays(3), baseline, Today));
    }

    [Fact]
    public void The_ai_cannot_schedule_into_the_past()
    {
        var baseline = Today.AddDays(2);
        Assert.Equal(Today, AiCycleReview.ClampToBaseline(Today.AddDays(-5), baseline, Today));
    }

    [Fact]
    public void A_spray_never_goes_before_the_ring_weeding_it_follows()
    {
        var baselineSpray = new CycleResult(CycleStatus.OnTrack, Today.AddDays(6), null, "", [], ConfidenceLevel.High,
            NeedsWeedingFirst: true, EarliestDate: Today.AddDays(6));
        // AI moves weeding later (day 9); spray must be at least 2 days after it.
        Assert.Equal(Today.AddDays(11), AiCycleReview.EnforceRingWeedingOrder(Today.AddDays(7), baselineSpray, Today.AddDays(9), 2));
        // AI tries to spray before the baseline earliest date.
        Assert.Equal(Today.AddDays(6), AiCycleReview.EnforceRingWeedingOrder(Today.AddDays(2), baselineSpray, Today.AddDays(4), 2));
    }

    [Fact]
    public void Reschedule_redoes_status_and_wording()
    {
        var r = new CycleResult(CycleStatus.OnTrack, Today.AddDays(30), null, "Weeding due in 4 weeks", [], ConfidenceLevel.High);
        var moved = BlockCycleCalculator.Reschedule(r, "Weeding", Today.AddDays(3), Today);
        Assert.Equal(CycleStatus.DueSoon, moved.Status);
        Assert.Equal("Weeding due in 3 days", moved.Headline);
    }
}

public class SprayDaySnapTests
{
    private static readonly DateTime Today = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    private static DailyWeather Day(int offset, double mm, double wind = 8, int chance = 10) =>
        new(Today.AddDays(offset), mm, 30, 22, wind, 0, chance);

    [Fact]
    public void A_good_day_is_left_alone()
    {
        var forecast = new[] { Day(0, 0), Day(1, 0), Day(2, 0) };
        var (d, changed) = AiCycleReview.SnapToSprayableDay(Today.AddDays(1), Today.AddDays(2), Today, forecast, Today);
        Assert.Equal(Today.AddDays(1), d);
        Assert.False(changed);
    }

    [Fact]
    public void A_wet_day_moves_to_the_nearest_dry_calm_day_in_range()
    {
        var forecast = new[] { Day(0, 20, chance: 90), Day(1, 30, chance: 90), Day(2, 0.5), Day(3, 0) };
        var (d, changed) = AiCycleReview.SnapToSprayableDay(Today.AddDays(1), Today.AddDays(2), Today, forecast, Today);
        Assert.Equal(Today.AddDays(2), d);
        Assert.True(changed);
    }

    [Fact]
    public void With_no_dry_day_in_range_the_rules_date_stands()
    {
        var forecast = Enumerable.Range(0, 16).Select(i => Day(i, 15, chance: 90)).ToArray();
        var (d, changed) = AiCycleReview.SnapToSprayableDay(Today, Today.AddDays(2), Today, forecast, Today);
        Assert.Equal(Today.AddDays(2), d);
        Assert.True(changed);   // it moved from the AI's date back to the rules' date
    }

    [Fact]
    public void Dates_beyond_the_forecast_are_not_judged()
    {
        var forecast = new[] { Day(0, 0) };
        var (d, changed) = AiCycleReview.SnapToSprayableDay(Today.AddDays(30), Today.AddDays(30), Today, forecast, Today);
        Assert.Equal(Today.AddDays(30), d);
        Assert.False(changed);
    }
}
