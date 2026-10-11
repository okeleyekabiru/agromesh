using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.ValueObjects;

namespace AgroMesh.Application.Services;

public enum CycleStatus { Unknown, NotNeeded, OnTrack, DueSoon, Overdue }

/// <summary>When one kind of work is next due on a block, and why.</summary>
public record CycleResult(
    CycleStatus Status,
    DateTime? DueDate,
    DateTime? LastDone,
    string Headline,
    List<string> Factors,
    ConfidenceLevel Confidence,
    DateTime? RecommendedSprayDate = null,
    bool AiReviewed = false,
    // Spraying only: the block still has to be ring-weeded first, and the earliest day the spray can go ahead.
    bool NeedsWeedingFirst = false,
    DateTime? EarliestDate = null);

public record BlockCycle(CycleResult Weeding, CycleResult Spraying, CycleResult? Fertilizing = null);

public record CycleInputs(
    DateTime Today,
    DateTime? LastWeeding,
    DateTime? LastSpraying,
    string? LastChemical,
    bool WeatherKnown,
    double RainLast30Days,
    double ForecastRain16Days,
    IReadOnlyList<DailyWeather> Forecast,
    IReadOnlyList<DailyWeather> Historical,
    // For the fertilizer cycle: what grows in the block, when it was planted, and when it was last fed.
    IReadOnlyList<BlockCrop>? Crops = null,
    DateTime? LastFertilizing = null,
    int? PlantingYear = null);

/// <summary>
/// Works out the next weeding and spraying on a block from the weather and what was last done.
/// The rules, in plain terms:
/// <list type="bullet">
/// <item>Dry season (Harmattan, or a real dry spell): weeds barely grow, so no weeding is needed.</item>
/// <item>The more it has rained in the last 30 days (and the more rain is forecast), the faster weeds
/// grow, so weeding comes round sooner and spraying is more frequent.</item>
/// <item>A herbicide spray holds weeds back for 1-6 weeks (shorter in heavy rain, longer when dry, and
/// halved if it rained hard right after the spray), so weeding is not due until that has worn off.</item>
/// </list>
/// These are starting defaults, not agronomy gospel — they are all in one place to tune.
/// </summary>
public static class BlockCycleCalculator
{
    public static string SeasonOf(int month) => month switch
    {
        3 or 4 or 5 => "Early Rains",
        6 or 7 => "Peak Rains",
        8 => "August Break",
        9 or 10 or 11 => "Late Rains",
        _ => "Harmattan (Dry)"
    };

    /// <summary>The first day of the next rains: when weeding becomes relevant again after the dry season.</summary>
    public static DateTime NextRainsStart(DateTime today)
    {
        var year = today.Month >= 3 ? today.Year + 1 : today.Year;
        return new DateTime(year, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    }

    public static BlockCycle Calculate(CycleInputs i)
    {
        var season = SeasonOf(i.Today.Month);

        // Without a live forecast, fall back to what the season normally brings.
        var rain30 = i.WeatherKnown ? i.RainLast30Days : season switch
        {
            "Peak Rains" => 150,
            "Early Rains" or "Late Rains" => 100,
            "August Break" => 60,
            _ => 0,
        };

        var dry = season == "Harmattan (Dry)" || (i.WeatherKnown && rain30 < 20 && i.ForecastRain16Days < 10);

        var weeding = Weeding(i, season, rain30, dry);
        return new BlockCycle(weeding, Spraying(i, season, rain30, dry), Fertilizing(i, season, rain30, dry));
    }

    // ── weeding ──

    private static CycleResult Weeding(CycleInputs i, string season, double rain30, bool dry)
    {
        var factors = new List<string> { RainFactor(i, season, rain30) };

        if (dry)
        {
            factors.Add(season == "Harmattan (Dry)"
                ? "Harmattan: weeds barely grow, so no weeding is needed."
                : $"Dry spell: only {i.ForecastRain16Days:F0} mm forecast over the next 16 days.");
            return new CycleResult(CycleStatus.NotNeeded, null, i.LastWeeding,
                "Not needed while it is dry", factors, ConfidenceLevel.High);
        }

        // Weeding and spraying are independent: a block can be sprayed without weeding first, so only the
        // last weeding counts here.
        if (i.LastWeeding is not { } weeded)
            return new CycleResult(CycleStatus.Unknown, null, null,
                "No weeding logged yet", [.. factors, "Log a weeding to start tracking this block."],
                ConfidenceLevel.Low);

        // How often weeds need cutting, from how fast they are growing.
        var interval = rain30 switch { >= 150 => 5, >= 60 => 7, >= 30 => 10, _ => 12 };
        if (i.WeatherKnown && i.ForecastRain16Days > 100) { interval -= 1; factors.Add($"{i.ForecastRain16Days:F0} mm more rain is forecast: weeds will keep growing fast (1 week sooner)."); }
        else if (i.WeatherKnown && i.ForecastRain16Days < 10) { interval += 2; factors.Add("Little rain is forecast, so growth will slow (2 weeks later)."); }

        var due = weeded.Date.AddDays(interval * 7);
        factors.Add($"Last weeded {weeded:d MMM}: weeds usually need cutting again after about {interval} weeks.");

        var days = (due - i.Today.Date).Days;
        var status = days < 0 ? CycleStatus.Overdue : days <= 7 ? CycleStatus.DueSoon : CycleStatus.OnTrack;
        var confidence = i.WeatherKnown ? ConfidenceLevel.High : ConfidenceLevel.Low;

        return new CycleResult(status, due, weeded, Headline("Weeding", status, days), factors, confidence);
    }

    // ── spraying ──

    private static CycleResult Spraying(CycleInputs i, string season, double rain30, bool dry)
    {
        var factors = new List<string> { RainFactor(i, season, rain30) };

        if (i.LastSpraying is null)
            return new CycleResult(CycleStatus.Unknown, null, null,
                "No spraying logged yet", [.. factors, "Log a spraying to start tracking this block."], ConfidenceLevel.Low);

        // Rainy season: weeds and pests come back faster, so spraying is more frequent.
        var interval = rain30 switch { >= 150 => 4, >= 60 => 6, >= 30 => 8, _ => 12 };
        var sprayed = i.LastSpraying.Value;
        var due = sprayed.Date.AddDays(interval * 7);
        factors.Add($"Last sprayed {sprayed:d MMM}{(i.LastChemical is null ? "" : $" ({i.LastChemical})")}: " +
                    (dry ? $"dry weather, so about every {interval} weeks." : $"in this weather about every {interval} weeks."));

        var days = (due - i.Today.Date).Days;
        var status = days < 0 ? CycleStatus.Overdue : days <= 7 ? CycleStatus.DueSoon : CycleStatus.OnTrack;

        // Land the spray on a day it will not wash off.
        var best = BestSprayDay(i, due);
        if (best is { } b)
            factors.Add($"Best spray day in the forecast: {b:ddd d MMM} (dry and calm).");
        else if (i.WeatherKnown && days <= 16)
            factors.Add("No dry, calm day in the forecast yet: wait for a break in the rain.");

        var confidence = i.WeatherKnown ? ConfidenceLevel.High : ConfidenceLevel.Medium;
        return new CycleResult(status, due, sprayed, Headline("Spraying", status, days), factors, confidence, best);
    }

    // ── fertilizing ──

    /// <summary>How often a crop is fed, in weeks, and why. Starting defaults for Nigerian conditions.</summary>
    private static (int Weeks, string Note)? FertilizerProfile(CropType crop, int? plantingYear, int thisYear) => crop switch
    {
        CropType.OilPalm when plantingYear is { } y && thisYear - y < 3 =>
            (12, "young oil palm (under 3 years) is fed about every 3 months"),
        CropType.OilPalm => (26, "mature oil palm is fed about twice a year"),
        CropType.Maize => (6, "maize is fed in splits, about 6 weeks apart"),
        CropType.Cocoa => (17, "cocoa is fed about 3 times a year"),
        CropType.Cassava => (12, "cassava is fed about every 3 months"),
        _ => null,
    };

    private static string CropName(CropType c) => c switch
    {
        CropType.OilPalm => "Oil palm",
        _ => c.ToString(),
    };

    private static CycleResult Fertilizing(CycleInputs i, string season, double rain30, bool dry)
    {
        var thisYear = i.Today.Year;
        var crops = (i.Crops ?? []).Where(c => FertilizerProfile(c.CropType, i.PlantingYear, thisYear) is not null).ToList();
        if (crops.Count == 0)
            return new CycleResult(CycleStatus.Unknown, null, i.LastFertilizing,
                "Set the block's crops to plan fertilizer", ["Fertilizer timing depends on the crop: edit the block and choose what is growing."],
                ConfidenceLevel.Low);

        // The crops that take up most of the block set the pace; of those, the one fed most often wins.
        var main = crops.Where(c => c.Percentage >= 30).ToList();
        if (main.Count == 0) main = [crops.OrderByDescending(c => c.Percentage).First()];
        var weeks = main.Min(c => FertilizerProfile(c.CropType, i.PlantingYear, thisYear)!.Value.Weeks);

        var factors = new List<string>();
        foreach (var c in main)
        {
            var (w, note) = FertilizerProfile(c.CropType, i.PlantingYear, thisYear)!.Value;
            factors.Add($"{CropName(c.CropType)} ({c.Percentage}% of the block): {note} (every ~{w} weeks).");
        }
        if (main.Count > 1)
            factors.Add($"Mixed block: it follows the crop that needs feeding most often (every {weeks} weeks).");
        if (i.PlantingYear is null && main.Any(c => c.CropType == CropType.OilPalm))
            factors.Add("Planting year not set, so the palms are treated as mature. Add it to the block for a better timetable.");

        if (i.LastFertilizing is null)
            return new CycleResult(CycleStatus.Unknown, null, null, "No fertilizer logged yet",
                [.. factors, "Log a fertilizer application to start tracking this block."], ConfidenceLevel.Low);

        var last = i.LastFertilizing.Value;
        var due = last.Date.AddDays(weeks * 7);
        factors.Add($"Last fertilized {last:d MMM}: next feeding after about {weeks} weeks.");

        // Fertilizer needs moist soil: nothing is applied in the Harmattan, so it waits for the rains.
        if (due.Month is 12 or 1 or 2)
        {
            due = NextRainsStart(due);
            factors.Add("That falls in the dry Harmattan: wait for the rains in March.");
        }
        else if (dry && due.Date <= i.Today.Date.AddDays(14))
        {
            factors.Add("It is dry now: apply when the rain returns so the fertilizer is not wasted.");
        }

        if (i.WeatherKnown && i.ForecastRain16Days > 150)
            factors.Add($"Very wet ({i.ForecastRain16Days:F0} mm forecast): feed in a lighter-rain spell so it is not washed away.");
        factors.Add("Apply in the cleared ring right after weeding.");

        var best = BestFertilizerDay(i, due);
        if (best is { } b)
            factors.Add($"Best day in the forecast: {b:ddd d MMM} (light rain around, no downpour).");

        var days = (due.Date - i.Today.Date).Days;
        var status = days < 0 ? CycleStatus.Overdue : days <= 7 ? CycleStatus.DueSoon : CycleStatus.OnTrack;
        var confidence = i.WeatherKnown ? ConfidenceLevel.High : ConfidenceLevel.Medium;
        return new CycleResult(status, due.Date, last, Headline("Fertilizer", status, days), factors, confidence, best);
    }

    // Fertilizer wants some rain to carry it into the soil, but not a downpour that washes it off.
    private static DateTime? BestFertilizerDay(CycleInputs i, DateTime due)
    {
        if (!i.WeatherKnown) return null;
        var from = due.Date > i.Today.Date ? due.Date : i.Today.Date;
        var days = i.Forecast.OrderBy(d => d.Date).ToList();
        for (var k = 0; k < days.Count; k++)
        {
            if (days[k].Date.Date < from) continue;
            var nearRain = days.Skip(k).Take(3).Sum(d => d.PrecipitationMm);
            if (days[k].PrecipitationMm < 20 && days[k].WindspeedKmh < 30 && nearRain >= 3)
                return days[k].Date.Date;
        }
        return null;
    }

    // ── helpers ──

    private static DateTime? BestSprayDay(CycleInputs i, DateTime due)
    {
        if (!i.WeatherKnown) return null;
        var from = due.Date > i.Today.Date ? due.Date : i.Today.Date;
        return i.Forecast.Where(d => d.Date.Date >= from && IsGoodSprayDay(d)).Select(d => (DateTime?)d.Date.Date).FirstOrDefault();
    }

    /// <summary>Dry, calm and unlikely to rain: spray will not wash off or drift.</summary>
    public static bool IsGoodSprayDay(DailyWeather d) =>
        d.PrecipitationMm < 2 && d.WindspeedKmh < 20 && d.RainChance < 50;

    private static string RainFactor(CycleInputs i, string season, double rain30) => i.WeatherKnown
        ? $"{season}: {rain30:F0} mm of rain in the last 30 days — weeds are growing {(rain30 >= 150 ? "fast" : rain30 >= 60 ? "steadily" : rain30 >= 30 ? "slowly" : "very slowly")}."
        : $"{season} (no live weather): using the usual rainfall for the season.";

    /// <summary>The same result moved to a new due date (e.g. after the AI review), with status and wording redone.</summary>
    public static CycleResult Reschedule(CycleResult r, string work, DateTime newDue, DateTime today)
    {
        var days = (newDue.Date - today.Date).Days;
        var status = days < 0 ? CycleStatus.Overdue : days <= 7 ? CycleStatus.DueSoon : CycleStatus.OnTrack;
        var suffix = r.NeedsWeedingFirst ? " (ring-weed first)" : "";
        return r with { Status = status, DueDate = newDue.Date, Headline = Headline(work, status, days) + suffix };
    }

    private static string Headline(string work, CycleStatus status, int days) => status switch
    {
        CycleStatus.Overdue => $"{work} overdue by {-days} {(-days == 1 ? "day" : "days")}",
        CycleStatus.DueSoon when days == 0 => $"{work} due today",
        CycleStatus.DueSoon => $"{work} due in {days} {(days == 1 ? "day" : "days")}",
        _ => $"{work} due in {days / 7} {(days / 7 == 1 ? "week" : "weeks")}",
    };
}
