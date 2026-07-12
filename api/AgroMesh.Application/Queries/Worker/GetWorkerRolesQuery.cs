using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Worker;

public record GetWorkerRolesQuery(string FarmId) : IRequest<List<WorkerRoleDto>>;

public class GetWorkerRolesQueryHandler : IRequestHandler<GetWorkerRolesQuery, List<WorkerRoleDto>>
{
    private readonly IWorkerRoleRepository _roleRepo;
    private readonly IWorkerRepository _workerRepo;

    public GetWorkerRolesQueryHandler(IWorkerRoleRepository roleRepo, IWorkerRepository workerRepo)
    {
        _roleRepo = roleRepo;
        _workerRepo = workerRepo;
    }

    public async Task<List<WorkerRoleDto>> Handle(GetWorkerRolesQuery query, CancellationToken ct)
    {
        var roles = await _roleRepo.GetByFarmIdAsync(query.FarmId, ct);
        var results = new List<WorkerRoleDto>();

        foreach (var role in roles)
        {
            var count = await _workerRepo.GetCountByRoleAsync(role.Id, ct);
            results.Add(new WorkerRoleDto(role.Id, role.FarmId, role.Title, role.DefaultType, role.PayRate, role.PayFrequency, count));
        }

        return results;
    }
}
