using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using MediatR;

namespace AgroMesh.Application.Commands.WorkerRole;

public record CreateWorkerRoleCommand(string FarmId, CreateWorkerRoleRequest Request) : IRequest<WorkerRoleDto>;

public class CreateWorkerRoleCommandHandler : IRequestHandler<CreateWorkerRoleCommand, WorkerRoleDto>
{
    private readonly IWorkerRoleRepository _roleRepo;

    public CreateWorkerRoleCommandHandler(IWorkerRoleRepository roleRepo) => _roleRepo = roleRepo;

    public async Task<WorkerRoleDto> Handle(CreateWorkerRoleCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var role = Domain.Entities.WorkerRole.Create(command.FarmId, req.Title, req.DefaultType, req.PayRate, req.PayFrequency);
        await _roleRepo.AddAsync(role, ct);

        return new WorkerRoleDto(role.Id, role.FarmId, role.Title, role.DefaultType, role.PayRate, role.PayFrequency, 0);
    }
}
