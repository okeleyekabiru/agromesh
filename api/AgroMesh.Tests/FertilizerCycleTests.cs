using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class FertilizerCycleTests
{
    private static readonly DateTime Today = new(2026, 7, 10, 0, 0, 0, DateTimeKind.Utc);   // Peak Rains

    private static CycleResult Fert(DateTime? last, BlockCrop[] crops, int? plantingYear = null, DateTime? today = null,
        double rain30 = 120, double forecast16 = 60, IReadOnlyList<DailyWeather>? forecast = null) =>
        BlockCycleCalculator.Calculate(new CycleInputs(today ?? Today, null, null, null, true, rain30, forecast16,
            forecast ?? [], [], crops, last, plantingYear)).Fertilizing!;

    private static BlockCrop Crop(CropType t, int pct = 100) => new(t, 1, pct);

    [Fact]
    public void Mature_oil_palm_is_fed_about_twice_a_year()
    {
        var last = Today.AddDays(-120);
        var f = Fert(last, [Crop(CropType.OilPalm)], plantingYear: 2015);
        Assert.Equal(last.AddDays(26 * 7), f.DueDate);
    }

    [Fact]
    public void Young_oil_palm_is_fed_more_often()
    {
        var last = Today.AddDays(-30);
        var young = Fert(last, [Crop(CropType.OilPalm)], plantingYear: 2025);
        var mature = Fert(last, [Crop(CropType.OilPalm)], plantingYear: 2015);
        Assert.Equal(last.AddDays(12 * 7), young.DueDate);
        Assert.True(young.DueDate < mature.DueDate);
    }

    [Theory]
    [InlineData(CropType.Maize, 6)]
    [InlineData(CropType.Cocoa, 17)]
    [InlineData(CropType.Cassava, 12)]
    public void Each_crop_has_its_own_cycle(CropType crop, int weeks)
    {
        var last = Today.AddDays(-7);
        Assert.Equal(last.AddDays(weeks * 7), Fert(last, [Crop(crop)]).DueDate);
    }

    [Fact]
    public void A_mixed_block_follows_the_crop_fed_most_often()
    {
        var last = Today.AddDays(-7);
        var f = Fert(last, [Crop(CropType.OilPalm, 60), Crop(CropType.Maize, 40)], plantingYear: 2015);
        Assert.Equal(last.AddDays(6 * 7), f.DueDate);                       // maize (6 wks) beats palm (26 wks)
        Assert.Contains(f.Factors, x => x.StartsWith("Mixed block"));
    }

    [Fact]
    public void A_minor_crop_does_not_set_the_pace()
    {
        var last = Today.AddDays(-120);
        var f = Fert(last, [Crop(CropType.OilPalm, 80), Crop(CropType.Maize, 20)], plantingYear: 2015);
        Assert.Equal(last.AddDays(26 * 7), f.DueDate);                      // maize is only 20% of the block
    }

    [Fact]
    public void Fertilizer_waits_for_the_rains_instead_of_falling_in_the_harmattan()
    {
        var today = new DateTime(2026, 11, 1, 0, 0, 0, DateTimeKind.Utc);
        var last = today.AddDays(-100);                                    // 26 weeks later = early Feb
        var f = Fert(last, [Crop(CropType.OilPalm)], today: today, plantingYear: 2015);
        Assert.Equal(new DateTime(2027, 3, 1), f.DueDate);
        Assert.Contains(f.Factors, x => x.Contains("Harmattan"));
    }

    [Fact]
    public void A_block_with_no_fertilizer_logged_is_unknown_and_without_crops_asks_for_them()
    {
        Assert.Equal(CycleStatus.Unknown, Fert(null, [Crop(CropType.Maize)]).Status);
        Assert.Equal(CycleStatus.Unknown, Fert(Today.AddDays(-10), []).Status);
    }

    [Fact]
    public void The_best_day_is_just_before_rain_arrives_and_never_in_a_downpour()
    {
        var forecast = new[]
        {
            new DailyWeather(Today, 0, 30, 22, 8),
            new DailyWeather(Today.AddDays(1), 0, 30, 22, 8),
            new DailyWeather(Today.AddDays(2), 0, 30, 22, 8),
            new DailyWeather(Today.AddDays(3), 45, 26, 22, 10),             // downpour
            new DailyWeather(Today.AddDays(4), 6, 28, 22, 10),
        };
        var f = Fert(Today.AddDays(-200), [Crop(CropType.OilPalm)], plantingYear: 2015, forecast: forecast);
        Assert.Equal(Today.AddDays(1), f.RecommendedSprayDate);             // rain reaches it by day 3
    }

    [Fact]
    public void A_mixed_crop_type_must_be_spelled_out()
    {
        Assert.Throws<DomainException>(() => Block.Create("f1", "A", [new BlockCrop(CropType.Mixed, 1, 100)], 1));
    }

    [Fact]
    public void Planting_year_must_be_sensible()
    {
        Assert.Throws<DomainException>(() => Block.Create("f1", "A", [new BlockCrop(CropType.OilPalm, 1, 100)], 1, plantingYear: 1800));
        var ok = Block.Create("f1", "A", [new BlockCrop(CropType.OilPalm, 1, 100)], 1, plantingYear: 2018);
        Assert.Equal(2018, ok.PlantingYear);
    }
}
