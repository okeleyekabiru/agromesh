using System.Text.Json;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Services;

/// <summary>One kind of work as the AI sees it: a date (or none) and a short reason.</summary>
public record AiWorkOpinion(DateTime? DueDate, string Reason, ConfidenceLevel Confidence);

public record AiCycleOpinion(AiWorkOpinion? Weeding, AiWorkOpinion? Spraying);

/// <summary>
/// Reads the AI's answer and keeps it inside the guard rails: the rules give the date, the AI may only
/// nudge it a few days, and it can never put a spray before the ring weeding it depends on.
/// </summary>
public static class AiCycleReview
{
    public const int MaxShiftDays = 7;

    public static AiCycleOpinion? Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        if (start < 0 || end <= start) return null;

        try
        {
            using var doc = JsonDocument.Parse(text[start..(end + 1)]);
            return new AiCycleOpinion(
                ReadWork(doc.RootElement, "weeding"),
                ReadWork(doc.RootElement, "spraying"));
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static AiWorkOpinion? ReadWork(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var el) || el.ValueKind != JsonValueKind.Object) return null;

        DateTime? due = null;
        if (el.TryGetProperty("dueDate", out var d) && d.ValueKind == JsonValueKind.String
            && DateTime.TryParse(d.GetString(), out var parsed))
            due = DateTime.SpecifyKind(parsed.Date, DateTimeKind.Utc);

        var reason = el.TryGetProperty("reason", out var r) && r.ValueKind == JsonValueKind.String ? r.GetString() ?? "" : "";
        var confidence = el.TryGetProperty("confidence", out var c) && Enum.TryParse<ConfidenceLevel>(c.GetString(), true, out var level)
            ? level : ConfidenceLevel.Medium;
        return new AiWorkOpinion(due, reason.Trim(), confidence);
    }

    /// <summary>Keeps the AI's date within a week of the rules' date and never in the past.</summary>
    public static DateTime ClampToBaseline(DateTime ai, DateTime baseline, DateTime today)
    {
        var lo = baseline.Date.AddDays(-MaxShiftDays);
        var hi = baseline.Date.AddDays(MaxShiftDays);
        var clamped = ai.Date < lo ? lo : ai.Date > hi ? hi : ai.Date;
        return clamped < today.Date && baseline.Date >= today.Date ? today.Date : clamped;
    }

    /// <summary>
    /// A spray is only worth doing on a dry, calm day. If the AI picked a day the forecast says is wet or windy,
    /// move it to the nearest dry, calm day within the allowed range; if there is none, keep the rules' date
    /// (the spray waits for a dry window). Dates beyond the forecast are left alone.
    /// </summary>
    public static (DateTime Date, bool Changed) SnapToSprayableDay(
        DateTime chosen, DateTime baseline, DateTime earliest, IReadOnlyList<DailyWeather> forecast, DateTime today)
    {
        var day = forecast.FirstOrDefault(d => d.Date.Date == chosen.Date);
        if (day is null || BlockCycleCalculator.IsGoodSprayDay(day)) return (chosen.Date, false);

        var lo = new[] { today.Date, baseline.Date.AddDays(-MaxShiftDays), earliest.Date }.Max();
        var hi = baseline.Date.AddDays(MaxShiftDays);
        var near = forecast
            .Where(d => d.Date.Date >= lo && d.Date.Date <= hi && BlockCycleCalculator.IsGoodSprayDay(d))
            .Select(d => d.Date.Date)
            .OrderBy(d => Math.Abs((d - chosen.Date).Days))
            .Cast<DateTime?>().FirstOrDefault();

        if (near is { } snapped) return (snapped, snapped != chosen.Date);
        var keep = baseline.Date < earliest.Date ? earliest.Date : baseline.Date;
        return (keep, keep != chosen.Date);
    }

    /// <summary>A spray cannot come before the ring weeding it follows (plus the usual gap).</summary>
    public static DateTime EnforceRingWeedingOrder(DateTime spray, CycleResult baselineSpray, DateTime? weedingDue, int gapDays)
    {
        var result = spray.Date;
        if (baselineSpray.EarliestDate is { } earliest && result < earliest.Date) result = earliest.Date;
        if (baselineSpray.NeedsWeedingFirst && weedingDue is { } w && result < w.Date.AddDays(gapDays))
            result = w.Date.AddDays(gapDays);
        return result;
    }
}
