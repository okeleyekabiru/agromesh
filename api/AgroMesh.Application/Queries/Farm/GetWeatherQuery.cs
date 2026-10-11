using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Farm;

public record GetWeatherQuery(string FarmId) : IRequest<WeatherWidgetDto?>;

public class GetWeatherQueryHandler : IRequestHandler<GetWeatherQuery, WeatherWidgetDto?>
{
    private const int ForecastDays = 7;

    private readonly IFarmRepository _farmRepo;
    private readonly FarmWeatherProvider _weatherProvider;

    public GetWeatherQueryHandler(IFarmRepository farmRepo, FarmWeatherProvider weatherProvider)
    {
        _farmRepo = farmRepo;
        _weatherProvider = weatherProvider;
    }

    public async Task<WeatherWidgetDto?> Handle(GetWeatherQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new DomainException($"Farm '{query.FarmId}' not found.");

        var (weather, location) = await _weatherProvider.GetAsync(farm, ct);
        if (weather == null) return null;

        var today = weather.Forecast.FirstOrDefault();
        var days = weather.Forecast.Take(ForecastDays).Select(d => new DailyForecastDto(
            d.Date, d.PrecipitationMm, d.RainChance, d.TempMaxC, d.TempMinC, d.WindspeedKmh, d.WeatherCode,
            IsGoodSprayDay(d))).ToList();

        return new WeatherWidgetDto(
            weather.Latitude, weather.Longitude, weather.CurrentSeason,
            today?.PrecipitationMm ?? 0,
            today?.TempMaxC ?? 0,
            weather.NextDryWindowStart, weather.NextDryWindowEnd,
            weather.RainfallLast30Days,
            location, days);
    }

    /// <summary>Dry, calm and unlikely to rain: spray will not wash off or drift.</summary>
    public static bool IsGoodSprayDay(DailyWeather d) => BlockCycleCalculator.IsGoodSprayDay(d);
}
