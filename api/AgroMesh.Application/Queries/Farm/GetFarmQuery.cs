using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Queries.Farm;

public record GetFarmQuery(string FarmId) : IRequest<FarmDto>;

public class GetFarmQueryHandler : IRequestHandler<GetFarmQuery, FarmDto>
{
    private readonly IFarmRepository _farmRepo;
    private readonly IWorkerRepository _workerRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public GetFarmQueryHandler(IFarmRepository farmRepo, IWorkerRepository workerRepo, ICurrentUserAccessor currentUser)
    {
        _farmRepo = farmRepo;
        _workerRepo = workerRepo;
        _currentUser = currentUser;
    }

    public async Task<FarmDto> Handle(GetFarmQuery query, CancellationToken ct)
    {
        var farm = await _farmRepo.GetByIdWithBlocksAsync(query.FarmId, ct)
            ?? throw new NotFoundException("Farm", query.FarmId);

        // Don't reveal existence of farms outside the caller's company.
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", query.FarmId);

        var workers = await _workerRepo.GetByFarmIdAsync(farm.Id, activeOnly: true, ct);

        return GetFarmsQueryHandler.MapToDto(farm) with { WorkerCount = workers.Count };
    }
}
