using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Farm;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Farm;

public record SetFarmLocationCommand(string FarmId, SetFarmLocationRequest Request) : IRequest<FarmDto>;

public class SetFarmLocationCommandHandler : IRequestHandler<SetFarmLocationCommand, FarmDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public SetFarmLocationCommandHandler(IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<FarmDto> Handle(SetFarmLocationCommand command, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new NotFoundException("Farm", command.FarmId);

        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", command.FarmId);

        farm.SetLocation(command.Request.StateId, command.Request.LgaFreetext);
        await _farmRepo.UpdateAsync(farm, ct);

        return GetFarmsQueryHandler.MapToDto(farm);
    }
}
