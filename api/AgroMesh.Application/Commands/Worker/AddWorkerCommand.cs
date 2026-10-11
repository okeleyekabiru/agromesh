using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Worker;

public record AddWorkerCommand(string FarmId, AddWorkerRequest Request) : IRequest<WorkerDto>;

public class AddWorkerCommandHandler : IRequestHandler<AddWorkerCommand, WorkerDto>
{
    private readonly IWorkerRepository _workerRepo;
    private readonly IWorkerRoleRepository _roleRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public AddWorkerCommandHandler(
        IWorkerRepository workerRepo, IWorkerRoleRepository roleRepo,
        IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _workerRepo = workerRepo;
        _roleRepo = roleRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    /// <summary>Admin, FarmManager and Supervisor can add a worker; the legacy Member role cannot.</summary>
    public static bool CanAdd(UserRole role) => role is UserRole.Admin or UserRole.FarmManager or UserRole.Supervisor;

    public async Task<WorkerDto> Handle(AddWorkerCommand command, CancellationToken ct)
    {
        if (!CanAdd(_currentUser.Role))
            throw new ForbiddenException("You are not allowed to add workers.");

        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new NotFoundException("Farm", command.FarmId);
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", command.FarmId);

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Name)) throw new DomainException("Worker name is required.");

        var role = await _roleRepo.GetByIdAsync(req.WorkerRoleId, ct)
            ?? throw new DomainException($"Worker role '{req.WorkerRoleId}' not found.");
        if (role.FarmId != command.FarmId)
            throw new DomainException($"Worker role '{req.WorkerRoleId}' not found.");

        var worker = Domain.Entities.Worker.Create(command.FarmId, req.Name.Trim(), req.Initials, req.WorkerRoleId, req.Type);
        await _workerRepo.AddAsync(worker, ct);

        return new WorkerDto(worker.Id, worker.FarmId, worker.Name, worker.Initials,
            worker.WorkerRoleId, role.Title, role.PayRate, role.PayFrequency, worker.Type, worker.Active, 0);
    }
}
