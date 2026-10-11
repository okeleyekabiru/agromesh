using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Staff;

public record AddStaffUserCommand(AddStaffUserRequest Request) : IRequest<AuthUserDto>;

/// <summary>
/// Admin can add a FarmManager or Supervisor; a FarmManager can add a Supervisor only; everyone
/// else is forbidden. The new user joins the caller's company.
/// </summary>
public class AddStaffUserCommandHandler : IRequestHandler<AddStaffUserCommand, AuthUserDto>
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserAccessor _currentUser;

    public AddStaffUserCommandHandler(IUserRepository userRepo, IPasswordHasher passwordHasher, ICurrentUserAccessor currentUser)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public static bool CanAdd(UserRole caller, UserRole target) => caller switch
    {
        UserRole.Admin => target is UserRole.FarmManager or UserRole.Supervisor,
        UserRole.FarmManager => target is UserRole.Supervisor,
        _ => false,
    };

    public async Task<AuthUserDto> Handle(AddStaffUserCommand command, CancellationToken ct)
    {
        var req = command.Request;

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role) || role is not (UserRole.FarmManager or UserRole.Supervisor))
            throw new DomainException("Role must be 'FarmManager' or 'Supervisor'.");

        if (!CanAdd(_currentUser.Role, role))
            throw new ForbiddenException(role == UserRole.FarmManager
                ? "Only an admin can add a farm manager."
                : "You are not allowed to add users.");

        var companyId = _currentUser.CompanyId
            ?? throw new ForbiddenException("Your account is not attached to a company.");

        if (string.IsNullOrWhiteSpace(req.FullName)) throw new DomainException("Full name is required.");
        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@')) throw new DomainException("A valid email is required.");
        if (string.IsNullOrEmpty(req.Password) || req.Password.Length < 8) throw new DomainException("Password must be at least 8 characters.");

        if (await _userRepo.GetByEmailAsync(req.Email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        var user = Domain.Entities.User.CreateStaff(req.FullName.Trim(), req.Email, _passwordHasher.Hash(req.Password), companyId, role);
        await _userRepo.AddAsync(user, ct);
        await _userRepo.SaveAsync(ct);

        return new AuthUserDto(user.Id, user.Email, user.FullName, user.CompanyId, user.Role.ToString());
    }
}
