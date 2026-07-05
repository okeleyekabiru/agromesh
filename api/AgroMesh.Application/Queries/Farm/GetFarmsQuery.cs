using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Farm;

public record GetFarmsQuery : IRequest<List<FarmDto>>;

public class GetFarmsQueryHandler : IRequestHandler<GetFarmsQuery, List<FarmDto>>
{
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetFarmsQueryHandler(IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<List<FarmDto>> Handle(GetFarmsQuery query, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_currentUser.CompanyId))
            return [];

        var farms = await _farmRepo.GetByCompanyIdAsync(_currentUser.CompanyId, ct);
        return farms.Select(MapToDto).ToList();
    }

    public static FarmDto MapToDto(Domain.Entities.Farm f) => new(
        f.Id, f.Name, f.Location,
        AreaDto.From(f.TotalHectares, f.PreferredAreaUnit),
        f.OwnerId, f.CompanyId, f.CountryCode, f.StateId, f.LgaFreetext, f.SetupStatus,
        f.PreferredAreaUnit, f.CentroidLat, f.CentroidLng,
        f.Sections.Count, 0);
}
