using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Worker;

public record AddWorkerCommand(string FarmId, AddWorkerRequest Request) : IRequest<WorkerDto>;

public class AddWorkerCommandHandler : IRequestHandler<AddWorkerCommand, WorkerDto>
{
    private readonly IWorkerRepository _workerRepo;
    private readonly IWorkerRoleRepository _roleRepo;

    public AddWorkerCommandHandler(IWorkerRepository workerRepo, IWorkerRoleRepository roleRepo)
    {
        _workerRepo = workerRepo;
        _roleRepo = roleRepo;
    }

    public async Task<WorkerDto> Handle(AddWorkerCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var role = await _roleRepo.GetByIdAsync(req.WorkerRoleId, ct)
            ?? throw new DomainException($"Worker role '{req.WorkerRoleId}' not found.");

        var worker = Domain.Entities.Worker.Create(command.FarmId, req.Name, req.Initials, req.WorkerRoleId, req.Type);
        await _workerRepo.AddAsync(worker, ct);

        return new WorkerDto(worker.Id, worker.FarmId, worker.Name, worker.Initials,
            worker.WorkerRoleId, role.Title, role.DailyRate, worker.Type, worker.Active, 0);
    }
}
