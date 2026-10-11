using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.WorkerRole;

public record CreateWorkerRoleCommand(string FarmId, CreateWorkerRoleRequest Request) : IRequest<WorkerRoleDto>;

public class CreateWorkerRoleCommandHandler : IRequestHandler<CreateWorkerRoleCommand, WorkerRoleDto>
{
    private readonly IWorkerRoleRepository _roleRepo;
    private readonly IFarmRepository _farmRepo;
    private readonly ICurrentUserAccessor _currentUser;

    public CreateWorkerRoleCommandHandler(IWorkerRoleRepository roleRepo, IFarmRepository farmRepo, ICurrentUserAccessor currentUser)
    {
        _roleRepo = roleRepo;
        _farmRepo = farmRepo;
        _currentUser = currentUser;
    }

    /// <summary>Pay rates are sensitive: only Admin and FarmManager can create or change a wage band.</summary>
    public static bool CanManage(UserRole role) => role is UserRole.Admin or UserRole.FarmManager;

    public async Task<WorkerRoleDto> Handle(CreateWorkerRoleCommand command, CancellationToken ct)
    {
        if (!CanManage(_currentUser.Role))
            throw new ForbiddenException("Only an admin or farm manager can manage wage bands.");

        var farm = await _farmRepo.GetByIdAsync(command.FarmId, ct)
            ?? throw new NotFoundException("Farm", command.FarmId);
        if (!string.IsNullOrEmpty(_currentUser.CompanyId) && farm.CompanyId != _currentUser.CompanyId)
            throw new NotFoundException("Farm", command.FarmId);

        var req = command.Request;
        if (string.IsNullOrWhiteSpace(req.Title)) throw new DomainException("Title is required.");
        if (req.PayRate <= 0) throw new DomainException("Pay rate must be greater than zero.");

        var role = Domain.Entities.WorkerRole.Create(command.FarmId, req.Title.Trim(), req.DefaultType, req.PayRate, req.PayFrequency);
        await _roleRepo.AddAsync(role, ct);

        return new WorkerRoleDto(role.Id, role.FarmId, role.Title, role.DefaultType, role.PayRate, role.PayFrequency, 0);
    }
}
