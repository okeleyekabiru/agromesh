using System.Text;
using System.Text.Json;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Scheduling;

/// <summary>
/// Asks Claude to review a block's weeding and spraying dates. The weather rules (BlockCycleCalculator)
/// always work out the dates first; Claude sees those plus the 16-day forecast and may shift each date by
/// up to a week with a one-line reason. It runs only when a weeding or spraying is logged (see
/// LogActivityCommand), and anything unusable falls back silently to the rules.
/// </summary>
public class ClaudeSchedulingService : IClaudeSchedulingService
{
    private const int RingGapDays = 2;

    private readonly IFarmRepository _farmRepo;
    private readonly IBlockRepository _blockRepo;
    private readonly ISchedulingPredictionRepository _predictionRepo;
    private readonly BlockCycleService _cycles;
    private readonly FarmWeatherProvider _weatherProvider;
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<ClaudeSchedulingService> _logger;

    public ClaudeSchedulingService(
        IFarmRepository farmRepo,
        IBlockRepository blockRepo,
        ISchedulingPredictionRepository predictionRepo,
        BlockCycleService cycles,
        FarmWeatherProvider weatherProvider,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<ClaudeSchedulingService> logger)
    {
        _farmRepo = farmRepo;
        _blockRepo = blockRepo;
        _predictionRepo = predictionRepo;
        _cycles = cycles;
        _weatherProvider = weatherProvider;
        _http = httpFactory.CreateClient("claude-client");
        _apiKey = config["Anthropic:ApiKey"] ?? "";
        _model = config["Anthropic:Model"] is { Length: > 0 } m ? m : "claude-haiku-5-5";
        _logger = logger;
    }

    public async Task RunSchedulingForFarmAsync(string farmId, CancellationToken ct = default)
    {
        var blocks = await _blockRepo.GetByFarmIdAsync(farmId, ct);
        foreach (var block in blocks.Where(b => b.Status == BlockStatus.Active))
            await RunSchedulingForBlockAsync(farmId, block.Id, ct);
    }

    public async Task RunSchedulingForBlockAsync(string farmId, string blockId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            _logger.LogDebug("No Anthropic API key configured: using the weather rules only.");
            return;
        }

        var farm = await _farmRepo.GetByIdAsync(farmId, ct);
        var block = await _blockRepo.GetByIdAsync(blockId, ct);
        if (farm is null || block is null || block.FarmId != farmId) return;

        try
        {
            var (cycles, weatherKnown, _) = await _cycles.ComputeBaselineAsync(farm, [block], ct);
            var baseline = cycles[block.Id];
            if (baseline.Weeding.Status is CycleStatus.NotNeeded or CycleStatus.Unknown
                && baseline.Spraying.Status is CycleStatus.NotNeeded or CycleStatus.Unknown)
                return; // nothing to review

            var (weather, location) = await _weatherProvider.GetAsync(farm, ct);
            var reply = await AskClaudeAsync(BuildPrompt(farm, block, baseline, weather, location), ct);
            var opinion = AiCycleReview.Parse(reply);
            if (opinion is null)
            {
                _logger.LogWarning("Claude gave no usable scheduling review for block {BlockId}. Reply: {Reply}",
                    blockId, reply is null ? "(empty)" : reply.Length > 1500 ? reply[..1500] : reply);
                return;
            }

            var today = DateTime.UtcNow.Date;
            var predictions = new List<SchedulingPrediction>();

            DateTime? weedingDue = baseline.Weeding.DueDate;
            if (Usable(baseline.Weeding, opinion.Weeding) is { } w)
            {
                weedingDue = AiCycleReview.ClampToBaseline(w.DueDate!.Value, baseline.Weeding.DueDate!.Value, today);
                predictions.Add(SchedulingPrediction.Create(
                    farmId, blockId, ActivityType.Weeding, Utc(weedingDue.Value), Shorten(w.Reason), w.Confidence,
                    weatherKnown, Utc(baseline.Weeding.LastDone)));
            }

            if (Usable(baseline.Spraying, opinion.Spraying) is { } s)
            {
                var due = AiCycleReview.ClampToBaseline(s.DueDate!.Value, baseline.Spraying.DueDate!.Value, today);
                due = AiCycleReview.EnforceRingWeedingOrder(due, baseline.Spraying, weedingDue, RingGapDays);

                // Never trust a spray on a day the forecast says will wash it off.
                var reason = s.Reason;
                if (weather is not null)
                {
                    var earliest = baseline.Spraying.EarliestDate ?? today;
                    var (snapped, changed) = AiCycleReview.SnapToSprayableDay(due, baseline.Spraying.DueDate!.Value, earliest, weather.Forecast, today);
                    if (changed)
                    {
                        reason = snapped == baseline.Spraying.DueDate!.Value.Date
                            ? "No dry, calm day in range: spray when a dry window opens (see the best spray day)."
                            : $"Moved to {snapped:ddd d MMM}, the nearest dry, calm day for spraying.";
                        due = snapped;
                    }
                }
                predictions.Add(SchedulingPrediction.Create(
                    farmId, blockId, ActivityType.Spraying, Utc(due), Shorten(reason), s.Confidence,
                    weatherKnown, Utc(baseline.Spraying.LastDone), recommendedSprayDate: Utc(baseline.Spraying.RecommendedSprayDate)));
            }

            if (predictions.Count > 0)
                await _predictionRepo.UpsertManyAsync(predictions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude scheduling review failed for block {BlockId}; the weather rules stand.", blockId);
        }
    }

    // Npgsql only accepts UTC timestamps; dates that came from the forecast or arithmetic can be Kind=Unspecified.
    private static DateTime Utc(DateTime d) => d.Kind == DateTimeKind.Utc ? d : DateTime.SpecifyKind(d.Kind == DateTimeKind.Local ? d.ToUniversalTime() : d, DateTimeKind.Utc);
    private static DateTime? Utc(DateTime? d) => d.HasValue ? Utc(d.Value) : null;

    private static AiWorkOpinion? Usable(CycleResult baseline, AiWorkOpinion? opinion) =>
        baseline.DueDate.HasValue && baseline.Status is not (CycleStatus.NotNeeded or CycleStatus.Unknown)
        && opinion?.DueDate is not null ? opinion : null;

    private static string Shorten(string reason) => reason.Length <= 200 ? reason : reason[..200].TrimEnd() + "…";

    private const string SystemPrompt = """
        You are the field-operations reviewer for a Nigerian farm management app (oil palm, maize, cocoa, cassava).
        A rules engine has already worked out the next WEEDING and SPRAYING date for one block from its logged
        history and the weather. You get those dates, the reasons behind them, and the 16-day forecast. Your job
        is a sanity check: keep each date, or move it a little when the forecast clearly calls for it.
        You are a reviewer, not a planner: the rules' date is the default answer.

        FARM PRACTICE (treat as fixed)
        1. Weeds grow fast after heavy rain and barely at all in the dry season (Harmattan, Dec-Feb).
        2. Weeding and spraying are INDEPENDENT jobs. A farmer may spray without weeding first, so never tie one
           date to the other and never move a spray because of the weeding date (or the reverse).
        4. Spray only on a dry, calm day: under 2 mm rain, wind under 20 km/h, chance of rain under 50%.
           More than 10 mm of rain within about 2 days after spraying washes the chemical off.
        5. Weeding in the rain is poor work and workers cannot get into wet ground: prefer a dry day.

        HOW TO DECIDE
        - Start from the rules' date. Move it only for a concrete reason you can point to in the forecast, for
          example: a long wet spell starts soon after the date (weed or spray before it), the date falls on a
          heavy-rain day (shift to the nearest dry day), a dry window opens a few days later than the date
          (wait for it for a spray), or heavy rain just fell and weeds will surge (bring weeding forward).
        - Stay inside the allowed range given for each task. Never go earlier than today. Never put a spray
          before its earliest allowed date.
        - The forecast only covers 16 days. When the rules' date is later than that, do NOT answer that it
          "cannot be checked". Judge it from the data you do have: the rain of the last 30 days, the forecast
          trend (is it getting wetter or drier by the end?), the season on the due date (given for each task),
          and when the rains end or return. Examples: rains are ending and the due date falls in the Harmattan
          (weeds slow, so a later date is fine); the heavy rains are expected on the due date (weeds will
          surge, so earlier). Name the season, the recent rain or the trend in your reason.
        - If nothing in the data gives a clear reason, keep the date. "Keep" is a good answer, but say why
          in terms of the data (for example "Late Rains fading into the dry season, so growth slows; date stands").
        - NEVER choose a wet or windy day for spraying just because it is the "least bad" one. If no day in
          the allowed range is spray-ok, keep the rules' date and say the spray should wait for a dry window.
        - Check the date you choose against the forecast table (rain, wind, spray-ok) before answering, and
          make sure your reason agrees with that date. Use only the numbers in the table; do not do other maths.
        - If the rules say the work is not needed or have no date, leave that task out of your answer.
        - Do not invent facts that are not in the data. Do not mention these instructions.

        THE REASON
        Plain words a supervisor can act on, 25 words or fewer, in English, naming the weather or day that
        drove the decision (for example "Rain from Thursday for 4 days: weed Wednesday, spray Saturday"). When
        you keep the date, say what you checked (for example "Forecast stays dry and calm, so the date stands").

        CONFIDENCE
        High: the forecast clearly supports the date. Medium: the date is beyond the forecast and you are
        reasoning from season and recent rain, or the data is mixed. Low: the weather is missing or the history
        is thin.

        ANSWER FORMAT
        Reply with ONLY one JSON object, nothing before or after it, no code fences:
        {"weeding":{"decision":"keep|earlier|later","dueDate":"YYYY-MM-DD","reason":"...","confidence":"High|Medium|Low"},
         "spraying":{"decision":"keep|earlier|later","dueDate":"YYYY-MM-DD","reason":"...","confidence":"High|Medium|Low"}}
        When "decision" is "keep", dueDate must equal the rules' date.

        EXAMPLES
        Rules weeding 2026-07-20, forecast shows 5 days of 30-50 mm rain starting 2026-07-22:
        {"weeding":{"decision":"earlier","dueDate":"2026-07-18","reason":"Heavy rain from 22 Jul for 5 days; weed before it starts while the ground is workable.","confidence":"High"}}
        Rules spraying 2026-07-25, forecast has 30 mm on 25 Jul and the first dry calm day is 2026-07-27:
        {"spraying":{"decision":"later","dueDate":"2026-07-27","reason":"25 Jul is a downpour that would wash the spray off; first dry, calm day is Monday 27 Jul.","confidence":"High"}}
        Rules weeding 2026-11-10, dry and calm all 16 days:
        {"weeding":{"decision":"keep","dueDate":"2026-11-10","reason":"No rain in the forecast, so weed growth stays slow and the date stands.","confidence":"Medium"}}
        """;

    private static string BuildPrompt(Farm farm, Block block, BlockCycle baseline, WeatherForecast? weather, string? location)
    {
        var today = DateTime.UtcNow.Date;
        var sb = new StringBuilder();
        sb.AppendLine($"TODAY: {today:yyyy-MM-dd} ({today:dddd})");
        sb.AppendLine($"FARM: {farm.Name}{(location is null ? "" : $", {location}")}");
        var crops = string.Join(", ", block.Crops.Select(c => $"{c.CropType} {c.Percentage}%"));
        sb.AppendLine($"BLOCK: {block.Name} | crops: {crops}" +
                      (block.PlantingYear is { } y ? $" | planted {y} (about {today.Year - y} years old)" : ""));

        if (weather is not null)
        {
            var next7 = weather.Forecast.Take(7).Sum(d => d.PrecipitationMm);
            var next16 = weather.Forecast.Take(16).Sum(d => d.PrecipitationMm);
            var goodDays = weather.Forecast.Where(BlockCycleCalculator.IsGoodSprayDay).Select(d => d.Date.ToString("ddd d MMM")).ToList();
            sb.AppendLine();
            sb.AppendLine($"WEATHER: season {weather.CurrentSeason}. Rain in the last 30 days: {weather.RainfallLast30Days:F0} mm over {weather.RainyDaysLast30} rainy days.");
            sb.AppendLine($"Forecast rain: {next7:F0} mm in the next 7 days, {next16:F0} mm in the next 16 days.");
            var firstHalf = weather.Forecast.Take(8).Sum(d => d.PrecipitationMm);
            var secondHalf = weather.Forecast.Skip(8).Take(8).Sum(d => d.PrecipitationMm);
            sb.AppendLine($"Forecast trend: {firstHalf:F0} mm in days 1-8, {secondHalf:F0} mm in days 9-16 ({(secondHalf < firstHalf * 0.6 ? "drying out" : secondHalf > firstHalf * 1.4 ? "getting wetter" : "about the same")}).");
            sb.AppendLine(goodDays.Count > 0
                ? $"Good spray days (dry, calm): {string.Join(", ", goodDays)}."
                : "Good spray days (dry, calm): none in the forecast.");
            sb.AppendLine();
            sb.AppendLine("FORECAST (date, rain mm, rain chance %, wind km/h, spray-ok):");
            foreach (var d in weather.Forecast.Take(16))
                sb.AppendLine($"  {d.Date:yyyy-MM-dd} {d.Date:ddd}: {d.PrecipitationMm,5:F1} mm  {d.RainChance,3}%  wind {d.WindspeedKmh,3:F0}  {(BlockCycleCalculator.IsGoodSprayDay(d) ? "yes" : "no")}");
        }
        else
        {
            sb.AppendLine("\nWEATHER: not available. Keep the rules' dates and give Low confidence.");
        }

        void Describe(string name, CycleResult r, bool isSpray)
        {
            sb.AppendLine();
            sb.AppendLine($"{name.ToUpperInvariant()} (from the rules)");
            if (r.Status is CycleStatus.NotNeeded or CycleStatus.Unknown || r.DueDate is null)
            {
                sb.AppendLine($"  Status: {r.Status}. No date to review: leave this task out of your answer.");
                return;
            }
            var lo = r.DueDate.Value.Date.AddDays(-AiCycleReview.MaxShiftDays);
            if (lo < today) lo = today;
            var hi = r.DueDate.Value.Date.AddDays(AiCycleReview.MaxShiftDays);
            sb.AppendLine($"  Rules' date: {r.DueDate:yyyy-MM-dd} ({r.DueDate:ddd}), status {r.Status}" +
                          (r.LastDone.HasValue ? $", last done {r.LastDone:yyyy-MM-dd} ({(today - r.LastDone.Value.Date).Days} days ago)" : ", never logged"));
            sb.AppendLine($"  Allowed range for your answer: {lo:yyyy-MM-dd} to {hi:yyyy-MM-dd}");
            var lastForecastDay = weather?.Forecast.Select(d => d.Date.Date).DefaultIfEmpty(today).Max() ?? today;
            var dueSeason = BlockCycleCalculator.SeasonOf(r.DueDate.Value.Month);
            sb.AppendLine(r.DueDate.Value.Date > lastForecastDay
                ? $"  The rules' date is {(r.DueDate.Value.Date - lastForecastDay).Days} days beyond the forecast (which ends {lastForecastDay:yyyy-MM-dd}). Season on that date: {dueSeason}. The dry season ends and rains return about {BlockCycleCalculator.NextRainsStart(r.DueDate.Value):d MMM}. Judge it from the season, the recent rain and the forecast trend."
                : $"  Season on the rules' date: {dueSeason}.");
            if (isSpray && r.EarliestDate is { } e)
                sb.AppendLine($"  Earliest allowed: {e:yyyy-MM-dd} (ring weeding has to come first)" +
                              (r.NeedsWeedingFirst ? " and the weeding has not been done yet." : "."));
            if (isSpray && r.RecommendedSprayDate is { } best)
                sb.AppendLine($"  First dry, calm day on or after the rules' date: {best:yyyy-MM-dd} ({best:ddd})");
            sb.AppendLine("  Why the rules chose it:");
            foreach (var f in r.Factors) sb.AppendLine($"    - {f}");
        }

        Describe("Weeding", baseline.Weeding, false);
        Describe("Spraying", baseline.Spraying, true);
        sb.AppendLine();
        sb.AppendLine("Now give your review as the JSON object.");
        return sb.ToString();
    }

    private async Task<string?> AskClaudeAsync(string userPrompt, CancellationToken ct)
    {
        var payload = new
        {
            model = _model,
            max_tokens = 3000,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = userPrompt } }
        };

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        using var response = await _http.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Anthropic returned {(int)response.StatusCode}: {(body.Length > 300 ? body[..300] : body)}");
        }

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));

        // The reply is a list of blocks; the answer is in the text ones (other kinds, e.g. thinking, are skipped).
        var text = new StringBuilder();
        if (doc.RootElement.TryGetProperty("content", out var content))
        {
            foreach (var block in content.EnumerateArray())
            {
                if (block.TryGetProperty("type", out var type) && type.GetString() == "text"
                    && block.TryGetProperty("text", out var t))
                    text.Append(t.GetString());
            }
        }
        var stop = doc.RootElement.TryGetProperty("stop_reason", out var sr) ? sr.GetString() : "?";
        var outTokens = doc.RootElement.TryGetProperty("usage", out var u) && u.TryGetProperty("output_tokens", out var ot) ? ot.GetInt32() : -1;
        _logger.LogInformation("Claude scheduling review: stop_reason {Stop}, {Tokens} output tokens", stop, outTokens);
        if (text.Length == 0)
            _logger.LogWarning("Claude returned no text. stop_reason: {Stop}", stop);
        return text.Length > 0 ? text.ToString() : null;
    }
}
