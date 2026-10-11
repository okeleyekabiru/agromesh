using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.WorkerRole;

public record UpdateWorkerRoleRateCommand(string RoleId, decimal NewPayRate, PayFrequency NewPayFrequency) : IRequest<Unit>;

public class UpdateWorkerRoleRateCommandHandler : IRequestHandler<UpdateWorkerRoleRateCommand, Unit>
{
    private readonly IWorkerRoleRepository _roleRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public UpdateWorkerRoleRateCommandHandler(IWorkerRoleRepository roleRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _roleRepo = roleRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    public async Task<Unit> Handle(UpdateWorkerRoleRateCommand command, CancellationToken ct)
    {
        if (!CreateWorkerRoleCommandHandler.CanManage(_currentUser.Role))
            throw new ForbiddenException("Only an admin or farm manager can manage wage bands.");

        var role = await _roleRepo.GetByIdAsync(command.RoleId, ct)
            ?? throw new DomainException($"Worker role '{command.RoleId}' not found.");

        var farm = await _farmRepo.GetByIdAsync(role.FarmId, ct)
            ?? throw new DomainException($"Worker role '{command.RoleId}' not found.");
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new DomainException($"Worker role '{command.RoleId}' not found.");
        if (command.NewPayRate <= 0)
            throw new DomainException("Pay rate must be greater than zero.");

        role.UpdatePay(command.NewPayRate, command.NewPayFrequency);
        await _roleRepo.UpdateAsync(role, ct);
        return Unit.Value;
    }
}
