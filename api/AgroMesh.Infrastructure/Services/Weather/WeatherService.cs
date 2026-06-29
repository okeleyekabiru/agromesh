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

    public WeatherService(IHttpClientFactory httpFactory, ILogger<WeatherService> logger, IConnectionMultiplexer? redis = null)
    {
        _http = httpFactory.CreateClient("weather-client");
        _logger = logger;
        _redis = redis;
    }

    public async Task<WeatherForecast?> GetFarmWeatherAsync(double lat, double lng, CancellationToken ct = default)
    {
        var cacheKey = $"weather:{lat:F4}:{lng:F4}";

        var cached = await TryGetFromCacheAsync(cacheKey, ct);
        if (cached != null) return cached;

        try
        {
            var forecastTask = FetchForecastAsync(lat, lng, ct);
            var historicalTask = FetchHistoricalAsync(lat, lng, ct);
            await Task.WhenAll(forecastTask, historicalTask);

            var forecast = await forecastTask;
            var historical = await historicalTask;

            if (forecast == null || historical == null) return null;

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

    private async Task<List<DailyWeather>?> FetchForecastAsync(double lat, double lng, CancellationToken ct)
    {
        var url = $"https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={lat}&longitude={lng}" +
                  $"&daily=precipitation_sum,temperature_2m_max,temperature_2m_min,windspeed_10m_max" +
                  $"&forecast_days=16&timezone=Africa%2FLagos";

        var json = await _http.GetStringAsync(url, ct);
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

        var json = await _http.GetStringAsync(url, ct);
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

        return dates.Select((d, i) => new DailyWeather(d, precip[i], tempMax[i], tempMin[i], wind[i])).ToList();
    }

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
