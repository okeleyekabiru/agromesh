using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Farm;

public record GetWeatherQuery(string FarmId) : IRequest<WeatherWidgetDto?>;

public class GetWeatherQueryHandler : IRequestHandler<GetWeatherQuery, WeatherWidgetDto?>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IWeatherService _weatherService;

    public GetWeatherQueryHandler(IFarmRepository farmRepo, IWeatherService weatherService)
    {
        _farmRepo = farmRepo;
        _weatherService = weatherService;
    }

    public async Task<WeatherWidgetDto?> Handle(GetWeatherQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(query.FarmId, ct)
            ?? throw new DomainException($"Farm '{query.FarmId}' not found.");

        if (!farm.CentroidLat.HasValue || !farm.CentroidLng.HasValue)
            return null;

        var weather = await _weatherService.GetFarmWeatherAsync(farm.CentroidLat.Value, farm.CentroidLng.Value, ct);
        if (weather == null) return null;

        var today = weather.Forecast.FirstOrDefault();
        return new WeatherWidgetDto(
            weather.Latitude, weather.Longitude, weather.CurrentSeason,
            today?.PrecipitationMm ?? 0,
            today?.TempMaxC ?? 0,
            weather.NextDryWindowStart, weather.NextDryWindowEnd,
            weather.RainfallLast30Days);
    }
}
