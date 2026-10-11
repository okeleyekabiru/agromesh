using System.Text.Json;
using AgroMesh.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace AgroMesh.Infrastructure.Services.Weather;

/// <summary>Open-Meteo's free geocoding API, restricted to Nigeria.</summary>
public class GeocodingService : IGeocodingService
{
    private readonly HttpClient _http;
    private readonly ILogger<GeocodingService> _logger;

    public GeocodingService(IHttpClientFactory httpFactory, ILogger<GeocodingService> logger)
    {
        _http = httpFactory.CreateClient("weather-client");
        _logger = logger;
    }

    public async Task<PlaceCoordinates?> FindPlaceAsync(string? place, string? region, CancellationToken ct = default)
    {
        try
        {
            // The local government area inside the state first, then the state itself.
            if (!string.IsNullOrWhiteSpace(place))
            {
                var hit = await SearchAsync(place.Trim(), region, ct);
                if (hit is not null) return hit;
            }
            if (!string.IsNullOrWhiteSpace(region))
                return await SearchAsync(region.Trim(), null, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Geocoding failed for {Place}, {Region}", place, region);
        }
        return null;
    }

    private async Task<PlaceCoordinates?> SearchAsync(string name, string? region, CancellationToken ct)
    {
        var url = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(name)}&count=10&language=en&format=json&countryCode=NG";
        using var doc = JsonDocument.Parse(await _http.GetStringAsync(url, ct));
        if (!doc.RootElement.TryGetProperty("results", out var results)) return null;

        foreach (var r in results.EnumerateArray())
        {
            if (region is not null)
            {
                var admin1 = r.TryGetProperty("admin1", out var a) ? a.GetString() ?? "" : "";
                if (!admin1.Contains(region, StringComparison.OrdinalIgnoreCase)) continue;
            }
            return new PlaceCoordinates(r.GetProperty("latitude").GetDouble(), r.GetProperty("longitude").GetDouble());
        }
        return null;
    }
}
