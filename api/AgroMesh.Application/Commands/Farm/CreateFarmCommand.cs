using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Utilities;
using MediatR;

namespace AgroMesh.Application.Commands.Farm;

public record CreateFarmCommand(CreateFarmRequest Request) : IRequest<FarmDto>;

public class CreateFarmCommandHandler : IRequestHandler<CreateFarmCommand, FarmDto>
{
    private readonly IFarmRepository _farmRepo;

    public CreateFarmCommandHandler(IFarmRepository farmRepo) => _farmRepo = farmRepo;

    public async Task<FarmDto> Handle(CreateFarmCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var hectares = AreaConverter.ToHectares(req.AreaValue, req.AreaUnit);

        var farm = Domain.Entities.Farm.Create(req.Name, req.Location, hectares, req.OwnerId, req.AreaUnit);
        await _farmRepo.AddAsync(farm, ct);

        return new FarmDto(
            farm.Id, farm.Name, farm.Location,
            AreaDto.From(farm.TotalHectares, farm.PreferredAreaUnit),
            farm.OwnerId, farm.PreferredAreaUnit,
            farm.CentroidLat, farm.CentroidLng, 0, 0);
    }
}
