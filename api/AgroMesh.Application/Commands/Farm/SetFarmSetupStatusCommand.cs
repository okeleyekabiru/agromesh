using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Farm;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Farm;

public record SetFarmSetupStatusCommand(string FarmId) : IRequest<FarmDto>;

public class SetFarmSetupStatusCommandHandler : IRequestHandler<SetFarmSetupStatusCommand, FarmDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public SetFarmSetupStatusCommandHandler(IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<FarmDto> Handle(SetFarmSetupStatusCommand command, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new NotFoundException("Farm", command.FarmId);

        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", command.FarmId);

        farm.MarkConfigured();
        await _farmRepo.UpdateAsync(farm, ct);

        return GetFarmsQueryHandler.MapToDto(farm);
    }
}
