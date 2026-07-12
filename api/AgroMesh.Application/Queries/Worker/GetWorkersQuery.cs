using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Queries.Worker;

public record GetWorkersQuery(string FarmId, bool ActiveOnly = true) : IRequest<List<WorkerDto>>;

public class GetWorkersQueryHandler : IRequestHandler<GetWorkersQuery, List<WorkerDto>>
{
    private readonly IWorkerRepository _workerRepo;

    public GetWorkersQueryHandler(IWorkerRepository workerRepo) => _workerRepo = workerRepo;

    public async Task<List<WorkerDto>> Handle(GetWorkersQuery query, CancellationToken ct)
    {
        var workers = await _workerRepo.GetByFarmIdAsync(query.FarmId, query.ActiveOnly, ct);
        return workers.Select(w => new WorkerDto(
            w.Id, w.FarmId, w.Name, w.Initials, w.WorkerRoleId,
            w.Role.Title, w.Role.PayRate, w.Role.PayFrequency, w.Type, w.Active, 0)).ToList();
    }
}
