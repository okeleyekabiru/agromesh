using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public record DailyWeather(
    DateTime Date,
    double PrecipitationMm,
    double TempMaxC,
    double TempMinC,
    double WindspeedKmh,
    int WeatherCode = 0,
    int RainChance = 0);

public record WeatherForecast(
    double Latitude,
    double Longitude,
    List<DailyWeather> Forecast,
    List<DailyWeather> Historical,
    double RainfallLast30Days,
    int RainyDaysLast30,
    string CurrentSeason,
    DateTime? NextDryWindowStart,
    DateTime? NextDryWindowEnd);

public record PlaceCoordinates(double Lat, double Lng);

/// <summary>Finds coordinates for a place name (e.g. a local government area within a state).</summary>
public interface IGeocodingService
{
    Task<PlaceCoordinates?> FindPlaceAsync(string? place, string? region, CancellationToken ct = default);
}

public interface IWeatherService
{
    Task<WeatherForecast?> GetFarmWeatherAsync(double lat, double lng, CancellationToken ct = default);
}
