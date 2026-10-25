using System.Collections.Concurrent;
using System.Text.Json;
using AgroMesh.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace AgroMesh.Infrastructure.Services.Weather;

public class WeatherService : IWeatherService
{
    private readonly HttpClient _http;
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<WeatherService> _logger;
    private const int CacheHours = 6;
    private static readonly TimeSpan StaleLimit = TimeSpan.FromHours(48);
    private static readonly TimeSpan FailureCooldown = TimeSpan.FromMinutes(2);

    // Open-Meteo rate-limits shared hosting addresses (HTTP 429), and without Redis nothing was cached, so every
    // page load asked again. These are per process: a copy in memory, a short pause after a failure, and one
    // request in flight per place.
    private static readonly ConcurrentDictionary<string, (DateTime At, WeatherForecast Data)> Memory = new();
    private static readonly ConcurrentDictionary<string, DateTime> Cooldown = new();
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    public WeatherService(IHttpClientFactory httpFactory, ILogger<WeatherService> logger, IConnectionMultiplexer? redis = null)
    {
        _http = httpFactory.CreateClient("weather-client");
        _logger = logger;
        _redis = redis;
    }

    public async Task<WeatherForecast?> GetFarmWeatherAsync(double lat, double lng, CancellationToken ct = default)
    {
        var cacheKey = $"weather2:{lat:F2}:{lng:F2}";

        if (Memory.TryGetValue(cacheKey, out var hit) && DateTime.UtcNow - hit.At < TimeSpan.FromHours(CacheHours))
            return hit.Data;

        var cached = await TryGetFromCacheAsync(cacheKey, ct);
        if (cached != null)
        {
            Memory[cacheKey] = (DateTime.UtcNow, cached);
            return cached;
        }

        var gate = Locks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(ct);
        try
        {
            // Someone else may have just fetched it while we waited.
            if (Memory.TryGetValue(cacheKey, out hit) && DateTime.UtcNow - hit.At < TimeSpan.FromHours(CacheHours))
                return hit.Data;

            // Recently refused: do not ask again straight away, show the last good forecast if there is one.
            if (Cooldown.TryGetValue(cacheKey, out var until) && until > DateTime.UtcNow)
                return StaleOrNull(cacheKey);

            var fresh = await FetchAsync(lat, lng, cacheKey, ct);
            if (fresh != null)
            {
                Memory[cacheKey] = (DateTime.UtcNow, fresh);
                Cooldown.TryRemove(cacheKey, out _);
                return fresh;
            }

            Cooldown[cacheKey] = DateTime.UtcNow + FailureCooldown;
            return StaleOrNull(cacheKey);
        }
        finally
        {
            gate.Release();
        }
    }

    private static WeatherForecast? StaleOrNull(string key) =>
        Memory.TryGetValue(key, out var old) && DateTime.UtcNow - old.At < StaleLimit ? old.Data : null;

    private async Task<WeatherForecast?> FetchAsync(double lat, double lng, string cacheKey, CancellationToken ct)
    {
        try
        {
            var forecastTask = TryAsync(() => FetchForecastAsync(lat, lng, ct), "forecast");
            var historicalTask = TryAsync(() => FetchHistoricalAsync(lat, lng, ct), "history");
            await Task.WhenAll(forecastTask, historicalTask);

            var forecast = await forecastTask;
            if (forecast == null) return null;
            // The forecast is the point of the widget: if only the past-30-days rainfall is unavailable, show the forecast without it.
            var historical = await historicalTask ?? [];

            var rainfallLast30 = historical.Sum(d => d.PrecipitationMm);
            var rainyDays = historical.Count(d => d.PrecipitationMm >= 1.0);
            var season = DetermineSeason(DateTime.UtcNow.Month);
            var (dryStart, dryEnd) = FindNextDryWindow(forecast);

            var result = new WeatherForecast(
                lat, lng, forecast, historical,
                rainfallLast30, rainyDays, season,
                dryStart, dryEnd);

            await TrySaveToCacheAsync(cacheKey, result, ct);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Open-Meteo weather fetch failed for {Lat},{Lng}", lat, lng);
            return null;
        }
    }

    /// <summary>GET with one retry when the service says "too many requests", waiting as long as it asks (up to a few seconds).</summary>
    private async Task<string> GetWithRetryAsync(string url, CancellationToken ct)
    {
        for (var attempt = 0; ; attempt++)
        {
            using var res = await _http.GetAsync(url, ct);
            if ((int)res.StatusCode == 429 && attempt < 1)
            {
                var wait = res.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(2);
                await Task.Delay(wait > TimeSpan.FromSeconds(4) ? TimeSpan.FromSeconds(4) : wait, ct);
                continue;
            }
            res.EnsureSuccessStatusCode();
            return await res.Content.ReadAsStringAsync(ct);
        }
    }

    private async Task<List<DailyWeather>?> TryAsync(Func<Task<List<DailyWeather>?>> fetch, string what)
    {
        try { return await fetch(); }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Open-Meteo {What} request failed", what);
            return null;
        }
    }

    private async Task<List<DailyWeather>?> FetchForecastAsync(double lat, double lng, CancellationToken ct)
    {
        var url = $"https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={lat}&longitude={lng}" +
                  $"&daily=precipitation_sum,temperature_2m_max,temperature_2m_min,windspeed_10m_max,weathercode,precipitation_probability_max" +
                  $"&forecast_days=16&timezone=Africa%2FLagos";

        var json = await GetWithRetryAsync(url, ct);
        return ParseDailyResponse(json);
    }

    private async Task<List<DailyWeather>?> FetchHistoricalAsync(double lat, double lng, CancellationToken ct)
    {
        var end = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        var start = end.AddDays(-30);
        var url = $"https://archive-api.open-meteo.com/v1/archive" +
                  $"?latitude={lat}&longitude={lng}" +
                  $"&start_date={start:yyyy-MM-dd}&end_date={end:yyyy-MM-dd}" +
                  $"&daily=precipitation_sum,temperature_2m_max,temperature_2m_min,windspeed_10m_max" +
                  $"&timezone=Africa%2FLagos";

        var json = await GetWithRetryAsync(url, ct);
        return ParseDailyResponse(json);
    }

    private static List<DailyWeather>? ParseDailyResponse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        if (!doc.RootElement.TryGetProperty("daily", out var daily)) return null;

        var dates = daily.GetProperty("time").EnumerateArray().Select(e => DateTime.Parse(e.GetString()!)).ToList();
        var precip = daily.GetProperty("precipitation_sum").EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Null ? 0.0 : e.GetDouble()).ToList();
        var tempMax = daily.GetProperty("temperature_2m_max").EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Null ? 0.0 : e.GetDouble()).ToList();
        var tempMin = daily.GetProperty("temperature_2m_min").EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Null ? 0.0 : e.GetDouble()).ToList();
        var wind = daily.GetProperty("windspeed_10m_max").EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Null ? 0.0 : e.GetDouble()).ToList();

        // Only the forecast endpoint returns these two; the historical archive does not.
        var codes = ReadInts(daily, "weathercode");
        var chance = ReadInts(daily, "precipitation_probability_max");

        return dates.Select((d, i) => new DailyWeather(d, precip[i], tempMax[i], tempMin[i], wind[i],
            i < codes.Count ? codes[i] : 0, i < chance.Count ? chance[i] : 0)).ToList();
    }

    private static List<int> ReadInts(JsonElement daily, string name) =>
        daily.TryGetProperty(name, out var arr)
            ? arr.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.Number ? (int)Math.Round(e.GetDouble()) : 0).ToList()
            : [];

    private static string DetermineSeason(int month) => month switch
    {
        3 or 4 or 5 => "Early Rains",
        6 or 7 => "Peak Rains",
        8 => "August Break",
        9 or 10 or 11 => "Late Rains",
        _ => "Harmattan (Dry)"
    };

    private static (DateTime? Start, DateTime? End) FindNextDryWindow(List<DailyWeather> forecast)
    {
        for (int i = 0; i <= forecast.Count - 3; i++)
        {
            if (forecast[i].PrecipitationMm < 2 &&
                forecast[i + 1].PrecipitationMm < 2 &&
                forecast[i + 2].PrecipitationMm < 2)
            {
                var end = i + 2;
                while (end + 1 < forecast.Count && forecast[end + 1].PrecipitationMm < 2) end++;
                return (forecast[i].Date, forecast[end].Date);
            }
        }
        return (null, null);
    }

    private async Task<WeatherForecast?> TryGetFromCacheAsync(string key, CancellationToken ct)
    {
        try
        {
            if (_redis == null) return null;
            var db = _redis.GetDatabase();
            var value = await db.StringGetAsync(key);
            if (!value.HasValue) return null;
            return JsonSerializer.Deserialize<WeatherForecast>(value!);
        }
        catch { return null; }
    }

    private async Task TrySaveToCacheAsync(string key, WeatherForecast forecast, CancellationToken ct)
    {
        try
        {
            if (_redis == null) return;
            var db = _redis.GetDatabase();
            var json = JsonSerializer.Serialize(forecast);
            await db.StringSetAsync(key, json, TimeSpan.FromHours(CacheHours));
        }
        catch { /* non-critical */ }
    }
}
