using System.Globalization;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Tests;

public class WorkCoverageTests
{
    // A block of about 100 m x 100 m (1 hectare) near Ibadan, with its south-west corner here.
    private const double Lat0 = 7.40, Lng0 = 3.90;
    private const double DegLat100M = 100 / 111_132.92;
    private static readonly double DegLng100M = 100 / (111_132.92 * Math.Cos(Lat0 * Math.PI / 180));

    private static GeoPoint At(double eastM, double northM) =>
        new(Lat0 + northM / 100 * DegLat100M, Lng0 + eastM / 100 * DegLng100M);

    private static string F(double v) => v.ToString("0.000000", CultureInfo.InvariantCulture);

    private static string RectGeoJson(double e0, double n0, double e1, double n1)
    {
        var pts = new[] { At(e0, n0), At(e1, n0), At(e1, n1), At(e0, n1), At(e0, n0) };
        return "{\"type\":\"Polygon\",\"coordinates\":[[" + string.Join(",", pts.Select(p => $"[{F(p.Lng)},{F(p.Lat)}]")) + "]]}";
    }

    private static readonly string Block = RectGeoJson(0, 0, 100, 100);

    // Eight points around a rectangle: the four corners and the four edge midpoints, in order.
    private static List<GeoPoint> Rect8(double e0, double n0, double e1, double n1)
    {
        var mx = (e0 + e1) / 2; var my = (n0 + n1) / 2;
        return [At(e0, n0), At(mx, n0), At(e1, n0), At(e1, my), At(e1, n1), At(mx, n1), At(e0, n1), At(e0, my)];
    }

    private static readonly DateTime D0 = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    // ── marking the area ──

    [Fact]
    public void Fewer_than_eight_points_are_not_enough() =>
        Assert.Throws<DomainException>(() => WorkCoverage.BuildWorkedArea(Rect8(0, 0, 50, 100).Take(7).ToList(), Block));

    [Fact]
    public void Half_the_block_measures_about_half_a_hectare()
    {
        var area = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 50, 100), Block);
        Assert.InRange(area.Hectares, 0.49, 0.51);
        Assert.False(area.ClippedToBlock);
    }

    [Fact]
    public void An_area_hanging_over_the_edge_is_cut_to_the_block()
    {
        var area = WorkCoverage.BuildWorkedArea(Rect8(50, 0, 150, 100), Block);   // half of it is outside
        Assert.InRange(area.Hectares, 0.49, 0.51);
        Assert.True(area.ClippedToBlock);
    }

    [Fact]
    public void An_area_outside_the_block_is_rejected() =>
        Assert.Throws<DomainException>(() => WorkCoverage.BuildWorkedArea(Rect8(200, 200, 300, 300), Block));

    [Fact]
    public void Points_that_cross_each_other_are_rejected()
    {
        // A bow-tie: corners visited in the wrong order.
        var pts = new List<GeoPoint> { At(0, 0), At(50, 0), At(100, 100), At(100, 50), At(100, 0), At(50, 100), At(0, 100), At(0, 50) };
        Assert.Throws<DomainException>(() => WorkCoverage.BuildWorkedArea(pts, Block));
    }

    [Fact]
    public void Points_all_in_one_spot_are_rejected()
    {
        var pts = Enumerable.Range(0, 8).Select(_ => At(10, 10)).ToList();
        Assert.Throws<DomainException>(() => WorkCoverage.BuildWorkedArea(pts, Block));
    }

    [Fact]
    public void A_block_without_a_boundary_still_measures_the_marked_area()
    {
        var area = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 50, 100), null);
        Assert.InRange(area.Hectares, 0.49, 0.51);
    }

    // ── done and left ──

    [Fact]
    public void Nothing_logged_means_not_started()
    {
        var c = WorkCoverage.Compute(Block, []);
        Assert.Equal(CoverageStatus.NotStarted, c.Status);
        Assert.Equal(0, c.Percent);
        Assert.InRange(c.RemainingHectares!.Value, 0.99, 1.01);
    }

    [Fact]
    public void Half_the_block_weeded_shows_half_done_and_half_left()
    {
        var half = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 50, 100), Block).GeoJson;
        var c = WorkCoverage.Compute(Block, [(D0, half)]);

        Assert.Equal(CoverageStatus.InProgress, c.Status);
        Assert.InRange(c.Percent, 49, 51);
        Assert.InRange(c.CoveredHectares, 0.49, 0.51);
        Assert.InRange(c.RemainingHectares!.Value, 0.49, 0.51);
        Assert.NotNull(c.CoveredGeoJson);
        Assert.NotNull(c.RemainingGeoJson);
    }

    [Fact]
    public void Two_halves_on_different_days_add_up_to_a_finished_cycle()
    {
        var left = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 50, 100), Block).GeoJson;
        var right = WorkCoverage.BuildWorkedArea(Rect8(50, 0, 100, 100), Block).GeoJson;
        var c = WorkCoverage.Compute(Block, [(D0, left), (D0.AddDays(2), right)]);

        Assert.Equal(CoverageStatus.Complete, c.Status);
        Assert.InRange(c.Percent, 99, 100);
        Assert.Null(c.RemainingGeoJson);
        Assert.Equal(2, c.ActivityCount);
    }

    [Fact]
    public void Overlapping_work_is_not_counted_twice()
    {
        var a = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 60, 100), Block).GeoJson;
        var b = WorkCoverage.BuildWorkedArea(Rect8(40, 0, 100, 100), Block).GeoJson;
        var c = WorkCoverage.Compute(Block, [(D0, a), (D0.AddDays(1), b)]);
        Assert.InRange(c.CoveredHectares, 0.99, 1.01);
    }

    [Fact]
    public void An_old_activity_without_an_outline_counts_as_the_whole_block()
    {
        var c = WorkCoverage.Compute(Block, [(D0, null)]);
        Assert.Equal(CoverageStatus.Complete, c.Status);
    }

    [Fact]
    public void Work_after_a_finished_cycle_starts_the_next_cycle()
    {
        var left = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 50, 100), Block).GeoJson;
        var c = WorkCoverage.Compute(Block, [(D0, null), (D0.AddDays(60), left)]);

        Assert.Equal(CoverageStatus.InProgress, c.Status);          // the new cycle is only half done
        Assert.InRange(c.Percent, 49, 51);
        Assert.Equal(D0.AddDays(60), c.CycleStartedOn);
        Assert.Equal(1, c.ActivityCount);
    }

    [Fact]
    public void A_half_done_cycle_keeps_its_coverage_however_long_it_waits()
    {
        var left = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 50, 100), Block).GeoJson;
        var right = WorkCoverage.BuildWorkedArea(Rect8(50, 0, 100, 100), Block).GeoJson;
        // The right half is done 200 days after the left: the left half is still counted, and that finishes the cycle.
        var c = WorkCoverage.Compute(Block, [(D0, left), (D0.AddDays(200), right)]);

        Assert.Equal(CoverageStatus.Complete, c.Status);
        Assert.InRange(c.Percent, 99, 100);
        Assert.Equal(D0, c.CycleStartedOn);
    }

    [Fact]
    public void A_block_without_a_boundary_reports_work_but_not_what_is_left()
    {
        var c = WorkCoverage.Compute(null, [(D0, null)]);
        Assert.False(c.HasBoundary);
        Assert.Null(c.RemainingHectares);
        Assert.Equal(CoverageStatus.InProgress, c.Status);
    }

    [Fact]
    public void Activities_can_arrive_in_any_order()
    {
        var left = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 50, 100), Block).GeoJson;
        var right = WorkCoverage.BuildWorkedArea(Rect8(50, 0, 100, 100), Block).GeoJson;
        var c = WorkCoverage.Compute(Block, [(D0.AddDays(2), right), (D0, left)]);
        Assert.Equal(CoverageStatus.Complete, c.Status);
    }

    [Fact]
    public void A_manual_completion_counts_as_the_whole_block_and_the_next_work_starts_a_new_cycle()
    {
        var shortOfEdge = WorkCoverage.BuildWorkedArea(Rect8(0, 0, 90, 100), Block).GeoJson;   // 90%: never reaches 95%
        var forced = D0.AddDays(3);

        var done = WorkCoverage.Compute(Block, [(D0, shortOfEdge), (forced, null)]);
        Assert.Equal(CoverageStatus.Complete, done.Status);

        var next = WorkCoverage.Compute(Block, [(D0, shortOfEdge), (forced, null), (D0.AddDays(60), shortOfEdge)]);
        Assert.Equal(CoverageStatus.InProgress, next.Status);
        Assert.Equal(D0.AddDays(60), next.CycleStartedOn);
    }
}
