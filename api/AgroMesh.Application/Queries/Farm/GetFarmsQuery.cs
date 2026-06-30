using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Utilities;
using MediatR;

namespace AgroMesh.Application.Queries.Farm;

public record GetFarmsQuery : IRequest<List<FarmDto>>;

public class GetFarmsQueryHandler : IRequestHandler<GetFarmsQuery, List<FarmDto>>
{
    private readonly IFarmRepository _farmRepo;

    public GetFarmsQueryHandler(IFarmRepository farmRepo) => _farmRepo = farmRepo;

    public async Task<List<FarmDto>> Handle(GetFarmsQuery query, CancellationToken ct)
    {
        var farms = await _farmRepo.GetAllAsync(ct);
        return farms.Select(f => new FarmDto(
            f.Id, f.Name, f.Location,
            AreaDto.From(f.TotalHectares, f.PreferredAreaUnit),
            f.OwnerId, f.PreferredAreaUnit,
            f.CentroidLat, f.CentroidLng,
            f.Sections.Count, 0)).ToList();
    }
}
