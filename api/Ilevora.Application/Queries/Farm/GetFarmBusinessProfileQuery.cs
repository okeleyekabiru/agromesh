using Ilevora.Application.DTOs;
using Ilevora.Application.Interfaces;
using Ilevora.Application.Services;
using MediatR;

namespace Ilevora.Application.Queries.Farm;

public record GetFarmBusinessProfileQuery(string FarmId) : IRequest<FarmBusinessProfileDto>;

public class GetFarmBusinessProfileQueryHandler : IRequestHandler<GetFarmBusinessProfileQuery, FarmBusinessProfileDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetFarmBusinessProfileQueryHandler(IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<FarmBusinessProfileDto> Handle(GetFarmBusinessProfileQuery query, CancellationToken ct) =>
        ToDto(await FarmAccess.GetFarmAsync(_farmRepo, _currentUser, query.FarmId, ct));

    public static FarmBusinessProfileDto ToDto(Domain.Entities.Farm f) =>
        new(f.Address, f.Phone, f.Email, f.Website, f.BankAccountName, f.BankAccountNumber, f.BankName, f.BrandPrimaryColor, f.BrandAccentColor);
}
