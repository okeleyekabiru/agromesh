using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Application.Queries.Farm;
using AgroMesh.Application.Services;
using MediatR;

namespace AgroMesh.Application.Commands.Farm;

public record UpdateFarmBusinessProfileCommand(string FarmId, UpdateFarmBusinessProfileRequest Request) : IRequest<FarmBusinessProfileDto>;

public class UpdateFarmBusinessProfileCommandHandler : IRequestHandler<UpdateFarmBusinessProfileCommand, FarmBusinessProfileDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateFarmBusinessProfileCommandHandler(IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<FarmBusinessProfileDto> Handle(UpdateFarmBusinessProfileCommand command, CancellationToken ct)
    {
        var farm = await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, command.FarmId, ct);
        FarmAccess.RequireManager(_currentUser, "change the farm business profile");

        var r = command.Request;
        // Omitted (null) colours keep what the farm has; "" resets to the template default.
        farm.SetBusinessProfile(r.Address, r.Phone, r.Email, r.Website, r.BankAccountName, r.BankAccountNumber, r.BankName,
            r.BrandPrimaryColor ?? farm.BrandPrimaryColor, r.BrandAccentColor ?? farm.BrandAccentColor);
        await _farmRepo.UpdateAsync(farm, ct);

        return GetFarmBusinessProfileQueryHandler.ToDto(farm);
    }
}
