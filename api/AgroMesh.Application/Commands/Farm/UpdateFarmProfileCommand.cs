using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Farm;
using AgroMesh.Domain.Exceptions;
using AgroMesh.Domain.Utilities;
using MediatR;

namespace AgroMesh.Application.Commands.Farm;

public record UpdateFarmProfileCommand(string FarmId, UpdateFarmProfileRequest Request) : IRequest<FarmDto>;

public class UpdateFarmProfileCommandHandler : IRequestHandler<UpdateFarmProfileCommand, FarmDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateFarmProfileCommandHandler(IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<FarmDto> Handle(UpdateFarmProfileCommand command, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new NotFoundException("Farm", command.FarmId);

        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", command.FarmId);

        var req = command.Request;
        var hectares = AreaConverter.ToHectares(req.AreaValue, req.AreaUnit);

        farm.Update(req.Name, farm.Location, hectares);
        farm.SetAreaUnit(req.AreaUnit);
        await _farmRepo.UpdateAsync(farm, ct);

        return GetFarmsQueryHandler.MapToDto(farm);
    }
}
