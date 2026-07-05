using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Farm;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Farm;

/// <summary>Flow A2 "Add New Farm" — for genuinely independent/distant land, not a Section under an existing Farm.</summary>
public record CreateFarmCommand(CreateFarmRequest Request) : IRequest<FarmDto>;

public class CreateFarmCommandHandler : IRequestHandler<CreateFarmCommand, FarmDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateFarmCommandHandler(IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<FarmDto> Handle(CreateFarmCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var companyId = _currentUser.CompanyId
            ?? throw new DomainException("You must belong to a company to add a farm.");

        var farm = Domain.Entities.Farm.Create(req.Name, companyId, _currentUser.UserId ?? string.Empty, req.StateId, req.LgaFreetext);
        await _farmRepo.AddAsync(farm, ct);

        return GetFarmsQueryHandler.MapToDto(farm);
    }
}
