using AgroMesh.Domain.Enums;

namespace AgroMesh.Application.Interfaces;

public record DailyWeather(
    DateTime Date,
    double PrecipitationMm,
    double TempMaxC,
    double TempMinC,
    double WindspeedKmh);

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

public interface IWeatherService
{
    Task<WeatherForecast?> GetFarmWeatherAsync(double lat, double lng, CancellationToken ct = default);
}
