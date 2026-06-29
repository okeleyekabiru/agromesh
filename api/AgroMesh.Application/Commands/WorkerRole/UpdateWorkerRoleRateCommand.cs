using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.WorkerRole;

public record UpdateWorkerRoleRateCommand(string RoleId, decimal NewDailyRate) : IRequest<Unit>;

public class UpdateWorkerRoleRateCommandHandler : IRequestHandler<UpdateWorkerRoleRateCommand, Unit>
{
    private readonly IWorkerRoleRepository _roleRepo;

    public UpdateWorkerRoleRateCommandHandler(IWorkerRoleRepository roleRepo) => _roleRepo = roleRepo;

    public async Task<Unit> Handle(UpdateWorkerRoleRateCommand command, CancellationToken ct)
    {
        var role = await _roleRepo.GetByIdAsync(command.RoleId, ct)
            ?? throw new DomainException($"Worker role '{command.RoleId}' not found.");

        role.UpdateRate(command.NewDailyRate);
        await _roleRepo.UpdateAsync(role, ct);
        return Unit.Value;
    }
}
