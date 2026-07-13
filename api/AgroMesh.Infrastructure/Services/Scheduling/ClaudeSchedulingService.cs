using System.Text;
using System.Text.Json;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Scheduling;

public class ClaudeSchedulingService : IClaudeSchedulingService
{
    private readonly IFarmRepository _farmRepo;
    private readonly ISectionRepository _sectionRepo;
    private readonly IActivityRepository _activityRepo;
    private readonly IInventoryRepository _inventoryRepo;
    private readonly ISchedulingPredictionRepository _predictionRepo;
    private readonly IWeatherService _weatherService;
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<ClaudeSchedulingService> _logger;

    private const string Model = "claude-sonnet-4-6";
    private const int MaxSectionsPerCall = 10;

    public ClaudeSchedulingService(
        IFarmRepository farmRepo,
        ISectionRepository sectionRepo,
        IActivityRepository activityRepo,
        IInventoryRepository inventoryRepo,
        ISchedulingPredictionRepository predictionRepo,
        IWeatherService weatherService,
        IHttpClientFactory httpFactory,
        IConfiguration config,
        ILogger<ClaudeSchedulingService> logger)
    {
        _farmRepo = farmRepo;
        _sectionRepo = sectionRepo;
        _activityRepo = activityRepo;
        _inventoryRepo = inventoryRepo;
        _predictionRepo = predictionRepo;
        _weatherService = weatherService;
        _http = httpFactory.CreateClient("claude-client");
        _apiKey = config["Anthropic:ApiKey"] ?? throw new InvalidOperationException("Anthropic:ApiKey is required");
        _logger = logger;
    }

    public async Task RunSchedulingForFarmAsync(string farmId, CancellationToken ct = default)
    {
        var farm = await _farmRepo.GetByIdWithSectionsAsync(farmId, ct);
        if (farm == null) return;

        var sections = farm.Sections.Where(s => s.Status == SectionStatus.Active).ToList();
        var batches = sections.Chunk(MaxSectionsPerCall);

        foreach (var batch in batches)
            await ProcessBatchAsync(farm, batch.ToList(), ct);
    }

    public async Task RunSchedulingForSectionAsync(string farmId, string sectionId, CancellationToken ct = default)
    {
        var farm = await _farmRepo.GetByIdAsync(farmId, ct);
        var section = await _sectionRepo.GetByIdAsync(sectionId, ct);
        if (farm == null || section == null) return;
        await ProcessBatchAsync(farm, [section], ct);
    }

    private async Task ProcessBatchAsync(Farm farm, List<Section> sections, CancellationToken ct)
    {
        var weatherTask = (farm.CentroidLat.HasValue && farm.CentroidLng.HasValue)
            ? _weatherService.GetFarmWeatherAsync(farm.CentroidLat.Value, farm.CentroidLng.Value, ct)
            : Task.FromResult<WeatherForecast?>(null);

        var inventoryTask = _inventoryRepo.GetByFarmIdAsync(farm.Id, ct);

        await Task.WhenAll(weatherTask, inventoryTask);
        var weather = await weatherTask;
        var inventory = await inventoryTask;

        var activityHistory = new Dictionary<string, (DateTime? lastWeeding, DateTime? lastSpraying, string? lastChemical)>();
        foreach (var section in sections)
        {
            var weedings = await _activityRepo.GetRecentBySectionAsync(section.Id, ActivityType.Weeding, 1, ct);
            var sprayings = await _activityRepo.GetRecentBySectionAsync(section.Id, ActivityType.Spraying, 1, ct);
            activityHistory[section.Id] = (
                weedings.FirstOrDefault()?.Date,
                sprayings.FirstOrDefault()?.Date,
                sprayings.FirstOrDefault()?.ChemicalProductName);
        }

        var prompt = BuildUserPrompt(farm, sections, weather, inventory, activityHistory);

        try
        {
            var predictions = await CallClaudeAsync(farm.Id, sections, prompt, weather != null, ct);
            await _predictionRepo.UpsertManyAsync(predictions, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Claude scheduling failed for farm {FarmId}", farm.Id);
        }
    }

    private static string SystemPrompt => """
        You are an agricultural scheduling assistant for Nigerian farms.
        Nigeria seasons: Early Rains (Mar–May), Peak Rains (Jun–Jul), August Break (Aug),
        Late Rains (Sep–Nov), Harmattan/Dry (Dec–Feb).

        Weed growth rules:
        - Harmattan: slow growth → extend weeding interval by 30–50%
        - Early/Late Rains: fast growth → standard or shorter interval
        - After >20mm rainfall in a week: accelerate next weeding by 5–10 days
        - If total rainfall last 30 days > 150mm: weeds aggressive, shorten interval

        Spraying rules:
        - NEVER recommend spraying within 24h before rain (>3mm forecast)
        - Ideal spray window: 3+ consecutive days with <2mm precipitation, wind <15 km/h
        - Herbicides in rain-saturated soil: effectiveness drops 40–60%

        Return ONLY valid JSON array. Never explain outside JSON.
        Each item must have exactly these fields:
        { "sectionId": string, "activityType": "Weeding"|"Spraying",
          "predictedNextDue": "YYYY-MM-DD", "reasoning": string (max 20 words),
          "recommendedChemical": string|null, "recommendedSprayDate": "YYYY-MM-DD"|null,
          "confidence": "High"|"Medium"|"Low" }
        """;

    private static string BuildUserPrompt(
        Farm farm,
        List<Section> sections,
        WeatherForecast? weather,
        List<InventoryItem> inventory,
        Dictionary<string, (DateTime? lastWeeding, DateTime? lastSpraying, string? lastChemical)> history)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Current date: {DateTime.UtcNow:yyyy-MM-dd}");
        sb.AppendLine($"Farm: {farm.Name}, {farm.Location}");

        if (weather != null)
        {
            sb.AppendLine($"Current season: {weather.CurrentSeason}");
            sb.AppendLine($"Rainfall last 30 days: {weather.RainfallLast30Days:F1}mm over {weather.RainyDaysLast30} rainy days");
            if (weather.NextDryWindowStart.HasValue)
                sb.AppendLine($"Next dry spray window: {weather.NextDryWindowStart:dd-MMM} to {weather.NextDryWindowEnd:dd-MMM}");

            sb.AppendLine("\n16-day forecast:");
            foreach (var d in weather.Forecast.Take(16))
                sb.AppendLine($"  {d.Date:ddd dd-MMM}: {d.PrecipitationMm:F1}mm rain, {d.TempMaxC:F0}°C, wind {d.WindspeedKmh:F0}km/h");
        }
        else
        {
            sb.AppendLine($"Current season: {DetermineSeason(DateTime.UtcNow.Month)} (no live weather data)");
        }

        var chemicals = inventory.Where(i => i.Category.Name == "Chemical" && i.CurrentStock > 0).ToList();
        if (chemicals.Any())
        {
            sb.AppendLine("\nAvailable chemicals:");
            foreach (var c in chemicals)
                sb.AppendLine($"  - {c.Name}: {c.CurrentStock} {c.Unit} in stock");
        }

        sb.AppendLine("\nSections to schedule:");
        foreach (var s in sections)
        {
            var (lastWeeding, lastSpraying, lastChem) = history.GetValueOrDefault(s.Id);
            var crops = string.Join(", ", s.Crops.Select(c => $"{c.CropType} {c.Percentage}%"));
            sb.AppendLine($"  Section: {s.Name} (id: {s.Id})");
            sb.AppendLine($"    Crops: {crops} | {s.Hectares:F1} ha");
            sb.AppendLine($"    Last weeding: {(lastWeeding.HasValue ? lastWeeding.Value.ToString("yyyy-MM-dd") : "never")}");
            sb.AppendLine($"    Last spraying: {(lastSpraying.HasValue ? lastSpraying.Value.ToString("yyyy-MM-dd") : "never")}" +
                          (lastChem != null ? $" ({lastChem})" : ""));
        }

        return sb.ToString();
    }

    private async Task<List<SchedulingPrediction>> CallClaudeAsync(
        string farmId, List<Section> sections, string userPrompt, bool weatherDataUsed, CancellationToken ct)
    {
        var payload = new
        {
            model = Model,
            max_tokens = 2000,
            system = SystemPrompt,
            messages = new[] { new { role = "user", content = userPrompt } }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
        request.Headers.Add("x-api-key", _apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await _http.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement
            .GetProperty("content")[0]
            .GetProperty("text")
            .GetString() ?? "[]";

        var items = JsonSerializer.Deserialize<List<JsonElement>>(text.Trim(), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                    ?? [];

        var predictions = new List<SchedulingPrediction>();
        foreach (var item in items)
        {
            var sectionId = item.GetProperty("sectionId").GetString() ?? "";
            var section = sections.FirstOrDefault(s => s.Id == sectionId);
            if (section == null) continue;

            if (!Enum.TryParse<ActivityType>(item.GetProperty("activityType").GetString(), out var actType)) continue;
            if (!DateTime.TryParse(item.GetProperty("predictedNextDue").GetString(), out var dueDate)) continue;
            if (!Enum.TryParse<ConfidenceLevel>(item.GetProperty("confidence").GetString(), out var conf)) conf = ConfidenceLevel.Medium;

            var reasoning = item.GetProperty("reasoning").GetString() ?? "";
            var chem = item.TryGetProperty("recommendedChemical", out var chemEl) && chemEl.ValueKind != JsonValueKind.Null ? chemEl.GetString() : null;
            DateTime? sprayDate = item.TryGetProperty("recommendedSprayDate", out var sprayEl) && sprayEl.ValueKind != JsonValueKind.Null
                ? DateTime.TryParse(sprayEl.GetString(), out var sd) ? sd : null
                : null;

            predictions.Add(SchedulingPrediction.Create(
                farmId, sectionId, actType, dueDate, reasoning, conf, weatherDataUsed,
                recommendedChemical: chem, recommendedSprayDate: sprayDate));
        }

        return predictions;
    }

    private static string DetermineSeason(int month) => month switch
    {
        3 or 4 or 5 => "Early Rains",
        6 or 7 => "Peak Rains",
        8 => "August Break",
        9 or 10 or 11 => "Late Rains",
        _ => "Harmattan (Dry)"
    };
}
