using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Services;

/// <summary>Finds a farm's weather: its saved coordinates, else its blocks' map centres, else a lookup of its area.</summary>
public class FarmWeatherProvider
{
    private readonly IBlockRepository _blockRepo;
    private readonly IStateRepository _stateRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly IGeocodingService _geocoding;
    private readonly IWeatherService _weather;

    public FarmWeatherProvider(
        IBlockRepository blockRepo, IStateRepository stateRepo, IFarmRepository farmRepo,
        IGeocodingService geocoding, IWeatherService weather)
    {
        _blockRepo = blockRepo;
        _stateRepo = stateRepo;
        _farmRepo = farmRepo;
        _geocoding = geocoding;
        _weather = weather;
    }

    public async Task<(WeatherForecast? Weather, string? Location)> GetAsync(Farm farm, CancellationToken ct)
    {
        var (point, label) = await ResolveLocationAsync(farm, ct);
        if (point is null) return (null, label);
        return (await _weather.GetFarmWeatherAsync(point.Lat, point.Lng, ct), label);
    }

    private async Task<(PlaceCoordinates? Point, string? Location)> ResolveLocationAsync(Farm farm, CancellationToken ct)
    {
        string? state = null;
        if (!string.IsNullOrEmpty(farm.StateId))
            state = (await _stateRepo.GetByIdAsync(farm.StateId, ct))?.Name;
        var place = string.Join(", ", new[] { farm.LgaFreetext?.Trim(), state }.Where(s => !string.IsNullOrWhiteSpace(s)));
        var label = place.Length > 0 ? place : null;

        if (farm.CentroidLat.HasValue && farm.CentroidLng.HasValue)
            return (new PlaceCoordinates(farm.CentroidLat.Value, farm.CentroidLng.Value), label);

        var centres = (await _blockRepo.GetByFarmIdAsync(farm.Id, ct))
            .Where(b => b.CentroidLat.HasValue && b.CentroidLng.HasValue && IsInNigeria(b.CentroidLat.Value, b.CentroidLng.Value))
            .ToList();
        if (centres.Count > 0)
            return (new PlaceCoordinates(centres.Average(b => b.CentroidLat!.Value), centres.Average(b => b.CentroidLng!.Value)), label);

        var found = await _geocoding.FindPlaceAsync(farm.LgaFreetext, state, ct);
        if (found is not null)
        {
            farm.SetCoordinates(found.Lat, found.Lng);
            await _farmRepo.UpdateAsync(farm, ct);
        }
        return (found, label);
    }

    // Rough bounding box of Nigeria; filters out boundaries captured with bad GPS points.
    private static bool IsInNigeria(double lat, double lng) => lat is > 4 and < 14.5 && lng is > 2.5 and < 15;
}

/// <summary>Computes every block's next weeding and spraying from the weather and what was last done.</summary>
public class BlockCycleService
{
    // An AI review is trusted for about as long as the forecast it was made from.
    public static readonly TimeSpan AiReviewLifetime = TimeSpan.FromDays(7);

    private readonly FarmWeatherProvider _weatherProvider;
    private readonly IActivityRepository _activityRepo;
    private readonly ISchedulingPredictionRepository _predictionRepo;

    public BlockCycleService(FarmWeatherProvider weatherProvider, IActivityRepository activityRepo, ISchedulingPredictionRepository predictionRepo)
    {
        _weatherProvider = weatherProvider;
        _activityRepo = activityRepo;
        _predictionRepo = predictionRepo;
    }

    /// <summary>The weather rules, with the AI's review applied where there is a current one.</summary>
    public async Task<(Dictionary<string, BlockCycle> Cycles, bool WeatherKnown)> ComputeAsync(Farm farm, IReadOnlyList<Block> blocks, CancellationToken ct)
    {
        var (cycles, weatherKnown, lastLogged) = await ComputeBaselineAsync(farm, blocks, ct);
        var predictions = await _predictionRepo.GetByFarmIdAsync(farm.Id, ct);
        var today = DateTime.UtcNow.Date;

        foreach (var block in blocks)
        {
            var cycle = cycles[block.Id];
            var logged = lastLogged.GetValueOrDefault(block.Id);

            CycleResult Apply(ActivityType type, string work, CycleResult baseline)
            {
                if (baseline.Status is CycleStatus.NotNeeded or CycleStatus.Unknown) return baseline;
                var p = predictions.FirstOrDefault(x => x.BlockId == block.Id && x.ActivityType == type);
                // Only a review made after the latest weeding / spraying, and still fresh, counts.
                if (p is null || p.ComputedAt < logged || DateTime.UtcNow - p.ComputedAt > AiReviewLifetime) return baseline;

                var moved = BlockCycleCalculator.Reschedule(baseline, work, p.PredictedNextDue, today);
                return moved with
                {
                    Factors = [$"🤖 AI review: {p.Reasoning}", .. baseline.Factors],
                    Confidence = p.Confidence,
                    RecommendedSprayDate = p.RecommendedSprayDate ?? baseline.RecommendedSprayDate,
                    AiReviewed = true,
                };
            }

            cycles[block.Id] = new BlockCycle(
                Apply(ActivityType.Weeding, "Weeding", cycle.Weeding),
                Apply(ActivityType.Spraying, "Spraying", cycle.Spraying),
                cycle.Fertilizing);
        }

        return (cycles, weatherKnown);
    }

    /// <summary>The weather rules alone (what the AI is asked to review), plus when each block was last weeded or sprayed.</summary>
    public async Task<(Dictionary<string, BlockCycle> Cycles, bool WeatherKnown, Dictionary<string, DateTime> LastLogged)> ComputeBaselineAsync(
        Farm farm, IReadOnlyList<Block> blocks, CancellationToken ct)
    {
        var (weather, _) = await _weatherProvider.GetAsync(farm, ct);
        var today = DateTime.UtcNow.Date;
        var result = new Dictionary<string, BlockCycle>();
        var lastLogged = new Dictionary<string, DateTime>();

        foreach (var block in blocks)
        {
            // Awaited one by one: the repositories share a single DbContext.
            var weeding = (await _activityRepo.GetRecentByBlockAsync(block.Id, ActivityType.Weeding, 1, ct)).FirstOrDefault();
            // Both spraying and pest control use chemicals; pest control does not hold weeds back, so only spraying counts.
            var spraying = (await _activityRepo.GetRecentByBlockAsync(block.Id, ActivityType.Spraying, 1, ct)).FirstOrDefault();
            var fertilizing = (await _activityRepo.GetRecentByBlockAsync(block.Id, ActivityType.FertilizerApplication, 1, ct)).FirstOrDefault();

            lastLogged[block.Id] = new[] { weeding?.CreatedAt, spraying?.CreatedAt }.Where(d => d.HasValue).Select(d => d!.Value).DefaultIfEmpty(DateTime.MinValue).Max();
            result[block.Id] = BlockCycleCalculator.Calculate(new CycleInputs(
                today, weeding?.Date, spraying?.Date, spraying?.ChemicalProductName,
                weather is not null,
                weather?.RainfallLast30Days ?? 0,
                weather?.Forecast.Take(16).Sum(d => d.PrecipitationMm) ?? 0,
                weather?.Forecast ?? [],
                weather?.Historical ?? [],
                block.Crops, fertilizing?.Date, block.PlantingYear));
        }

        return (result, weather is not null, lastLogged);
    }

    /// <summary>Shapes one result for the screens that already show an "upcoming task".</summary>
    public static UpcomingTaskDto ToTask(Block block, ActivityType type, CycleResult r, bool weatherKnown)
    {
        var today = DateTime.UtcNow.Date;
        // A block that does not need work yet still gets a date: when the rains come back.
        var due = r.DueDate ?? (r.Status == CycleStatus.NotNeeded ? BlockCycleCalculator.NextRainsStart(today) : today);
        return new UpcomingTaskDto(
            block.Id, block.Name, type, r.LastDone, due,
            (due.Date - today).Days,
            r.Headline,
            null, r.RecommendedSprayDate,
            r.Confidence, null, null, false, weatherKnown,
            r.Status.ToString(), r.Factors, r.AiReviewed);
    }
}
