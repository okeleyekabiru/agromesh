using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;
// NotFoundException gives callers a clean 404

namespace AgroMesh.Application.Queries.Farm;

public record GetFarmQuery(string FarmId) : IRequest<FarmDto>;

public class GetFarmQueryHandler : IRequestHandler<GetFarmQuery, FarmDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IWorkerRepository _workerRepo;

    public GetFarmQueryHandler(IFarmRepository farmRepo, IWorkerRepository workerRepo)
    {
        _farmRepo = farmRepo;
        _workerRepo = workerRepo;
    }

    public async Task<FarmDto> Handle(GetFarmQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdWithSectionsAsync(query.FarmId, ct)
            ?? throw new NotFoundException("Farm", query.FarmId);

        var workers = await _workerRepo.GetByFarmIdAsync(farm.Id, activeOnly: true, ct);

        return new FarmDto(
            farm.Id, farm.Name, farm.Location,
            AreaDto.From(farm.TotalHectares, farm.PreferredAreaUnit),
            farm.OwnerId, farm.PreferredAreaUnit,
            farm.CentroidLat, farm.CentroidLng,
            farm.Sections.Count, workers.Count);
    }
}
