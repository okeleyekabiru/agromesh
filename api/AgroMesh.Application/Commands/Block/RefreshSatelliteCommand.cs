using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Block;
using AgroMesh.Application.Services;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Block;

/// <summary>Fetches fresh satellite readings now. Allowed once an hour per farm, to protect the free quota.</summary>
public record RefreshSatelliteCommand(string FarmId) : IRequest<SatelliteOverviewDto>;

public class RefreshSatelliteCommandHandler : IRequestHandler<RefreshSatelliteCommand, SatelliteOverviewDto>
{
    private readonly IFarmRepository _farms;
    private readonly ISatelliteService _satellite;
    private readonly SatelliteRefresher _refresher;
    private readonly IMediator _mediator;

    public RefreshSatelliteCommandHandler(IFarmRepository farms, ISatelliteService satellite, SatelliteRefresher refresher, IMediator mediator)
    {
        _farms = farms;
        _satellite = satellite;
        _refresher = refresher;
        _mediator = mediator;
    }

    public async Task<SatelliteOverviewDto> Handle(RefreshSatelliteCommand command, CancellationToken ct)
    {
        var farm = await _farms.GetByIdAsync(command.FarmId, ct) ?? throw new NotFoundException("Farm", command.FarmId);
        if (!_satellite.IsConfigured)
            throw new DomainException("Satellite monitoring is not connected yet: add the Copernicus client id and secret to the API settings.");
        if (!farm.SatelliteSyncDue(DateTime.UtcNow, SatelliteRefresher.ManualEvery))
            throw new DomainException("Satellite readings were refreshed less than an hour ago. Try again later.");

        await _refresher.RefreshFarmAsync(farm, ct);
        return await _mediator.Send(new GetSatelliteOverviewQuery(command.FarmId), ct);
    }
}
