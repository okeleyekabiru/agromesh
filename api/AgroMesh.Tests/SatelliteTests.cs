using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;

namespace AgroMesh.Tests;

public class SatelliteTests
{
    private static readonly DateTime Today = new(2026, 10, 12, 0, 0, 0, DateTimeKind.Utc);

    private static BlockSatelliteReading R(int daysAgo, double ndvi, double ndmi, double valid = 1) =>
        BlockSatelliteReading.Create("f", "b", Today.AddDays(-daysAgo), ndvi, ndmi, valid);

    [Fact]
    public void No_readings_means_no_view_yet() =>
        Assert.Equal(CropCondition.NoData, SatelliteStatus.Evaluate([]).Condition);

    [Fact]
    public void Cloudy_readings_are_ignored() =>
        Assert.Equal(CropCondition.NoData, SatelliteStatus.Evaluate([R(0, 0.7, 0.3, valid: 0.1)]).Condition);

    [Fact]
    public void A_single_reading_has_nothing_to_be_compared_with()
    {
        var s = SatelliteStatus.Evaluate([R(0, 0.7, 0.3)]);
        Assert.Equal(CropCondition.Building, s.Condition);
        Assert.Equal(0.7, s.Ndvi);
    }

    [Fact]
    public void Steady_values_are_stable() =>
        Assert.Equal(CropCondition.Stable, SatelliteStatus.Evaluate([R(40, 0.70, 0.30), R(25, 0.71, 0.31), R(0, 0.72, 0.30)]).Condition);

    [Fact]
    public void A_drop_against_the_weeks_before_is_declining() =>
        Assert.Equal(CropCondition.Declining, SatelliteStatus.Evaluate([R(40, 0.75, 0.35), R(25, 0.74, 0.34), R(0, 0.60, 0.30)]).Condition);

    [Fact]
    public void A_rise_against_the_weeks_before_is_improving() =>
        Assert.Equal(CropCondition.Improving, SatelliteStatus.Evaluate([R(40, 0.55, 0.25), R(25, 0.56, 0.25), R(0, 0.70, 0.26)]).Condition);

    [Fact]
    public void Almost_no_leaf_water_is_very_dry_or_bare_whatever_the_history() =>
        Assert.Equal(CropCondition.DryOrBare, SatelliteStatus.Evaluate([R(40, 0.2, 0.01), R(0, 0.2, 0.01)]).Condition);

    [Fact]
    public void Readings_too_close_to_the_latest_do_not_count_as_the_baseline()
    {
        // The only earlier reading is 5 days before: too recent to say what the weeks before looked like.
        Assert.Equal(CropCondition.Building, SatelliteStatus.Evaluate([R(5, 0.9, 0.5), R(0, 0.5, 0.2)]).Condition);
    }

    [Fact]
    public void The_statistics_reply_is_read_and_cloudy_windows_are_dropped()
    {
        const string json = """
        {"data":[
          {"interval":{"from":"2026-09-01T00:00:00Z","to":"2026-09-06T00:00:00Z"},
           "outputs":{"ndvi":{"bands":{"B0":{"stats":{"mean":0.71234,"sampleCount":1000,"noDataCount":200}}}},
                      "ndmi":{"bands":{"B0":{"stats":{"mean":0.31,"sampleCount":1000,"noDataCount":200}}}}}},
          {"interval":{"from":"2026-09-06T00:00:00Z","to":"2026-09-11T00:00:00Z"},
           "outputs":{"ndvi":{"bands":{"B0":{"stats":{"mean":"NaN","sampleCount":1000,"noDataCount":1000}}}},
                      "ndmi":{"bands":{"B0":{"stats":{"mean":"NaN","sampleCount":1000,"noDataCount":1000}}}}}},
          {"interval":{"from":"2026-09-11T00:00:00Z","to":"2026-09-16T00:00:00Z"},
           "outputs":{"ndvi":{"bands":{"B0":{"stats":{"mean":0.6,"sampleCount":1000,"noDataCount":950}}}},
                      "ndmi":{"bands":{"B0":{"stats":{"mean":0.2,"sampleCount":1000,"noDataCount":950}}}}}}
        ],"status":"OK"}
        """;
        var rows = SatelliteStatistics.Parse(json);

        var row = Assert.Single(rows);                       // the all-cloud window and the 5%-clear window are gone
        Assert.Equal(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), row.From);
        Assert.Equal(0.7123, row.Ndvi);
        Assert.Equal(0.8, row.ValidShare);
    }

    [Fact]
    public void The_box_around_a_polygon_is_found()
    {
        var box = GeoBounds.Of("{\"type\":\"Polygon\",\"coordinates\":[[[3.90,7.40],[3.91,7.40],[3.91,7.41],[3.90,7.41],[3.90,7.40]]]}");
        Assert.NotNull(box);
        Assert.Equal(7.40, box!.MinLat);
        Assert.Equal(3.91, box.MaxLng);
        Assert.InRange(box.WidthMeters, 1000, 1200);
        Assert.InRange(box.HeightMeters, 1000, 1200);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"type\":\"Polygon\"}")]
    public void An_unreadable_outline_has_no_box(string? json) => Assert.Null(GeoBounds.Of(json));
}
