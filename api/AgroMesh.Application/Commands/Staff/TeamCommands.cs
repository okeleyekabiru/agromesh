using AgroMesh.Application.DTOs;
using AgroMesh.Application.Interfaces;
using AgroMesh.Domain.Enums;
using AgroMesh.Domain.Exceptions;
using MediatR;

namespace AgroMesh.Application.Commands.Staff;

public record TeamMemberDto(string Id, string FullName, string Email, string Role, bool IsActive, DateTime CreatedAt);

/// <summary>Admin only. Everyone in the caller's company.</summary>
public record GetTeamQuery() : IRequest<List<TeamMemberDto>>;

public class GetTeamQueryHandler : IRequestHandler<GetTeamQuery, List<TeamMemberDto>>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserAccessor _currentUser;

    public GetTeamQueryHandler(IUserRepository users, ICurrentUserAccessor currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<List<TeamMemberDto>> Handle(GetTeamQuery query, CancellationToken ct)
    {
        if (!_currentUser.IsAdmin) throw new ForbiddenException("Only an admin can see the team.");
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException("Your account is not attached to a company.");

        return (await _users.GetByCompanyAsync(companyId, ct))
            .Select(u => new TeamMemberDto(u.Id, u.FullName, u.Email, u.Role.ToString(), u.IsActive, u.CreatedAt))
            .ToList();
    }
}

public record ManagerOptionDto(string Id, string FullName);

/// <summary>
/// The active farm managers of the caller's company, by name. Open to every signed-in user: they appear as choices in an
/// activity's Supervisor list next to the supervisor workers, so whoever logs the work needs to see them (not emails or roles).
/// </summary>
public record GetManagerOptionsQuery() : IRequest<List<ManagerOptionDto>>;

public class GetManagerOptionsQueryHandler : IRequestHandler<GetManagerOptionsQuery, List<ManagerOptionDto>>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserAccessor _currentUser;

    public GetManagerOptionsQueryHandler(IUserRepository users, ICurrentUserAccessor currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<List<ManagerOptionDto>> Handle(GetManagerOptionsQuery query, CancellationToken ct)
    {
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException("Your account is not attached to a company.");
        return (await _users.GetByCompanyAsync(companyId, ct))
            .Where(u => u.Role == UserRole.FarmManager && u.IsActive)
            .Select(u => new ManagerOptionDto(u.Id, u.FullName))
            .ToList();
    }
}

public record SetUserActiveRequest(bool IsActive);

/// <summary>Admin only. Disables or re-enables someone in the caller's company. An admin cannot disable themselves or another admin.</summary>
public record SetUserActiveCommand(string UserId, bool IsActive) : IRequest<TeamMemberDto>;

public class SetUserActiveCommandHandler : IRequestHandler<SetUserActiveCommand, TeamMemberDto>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserAccessor _currentUser;

    public SetUserActiveCommandHandler(IUserRepository users, ICurrentUserAccessor currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<TeamMemberDto> Handle(SetUserActiveCommand command, CancellationToken ct)
    {
        if (!_currentUser.IsAdmin) throw new ForbiddenException("Only an admin can disable or enable users.");

        var user = await _users.GetByIdAsync(command.UserId, ct);
        // Same answer for "no such user" and "someone else's company", so ids of other companies are not revealed.
        if (user is null || user.CompanyId is null || user.CompanyId != _currentUser.CompanyId)
            throw new DomainException("User not found.");
        if (user.Id == _currentUser.UserId)
            throw new DomainException("You cannot disable your own account.");
        if (user.Role == UserRole.Admin)
            throw new DomainException("An admin account cannot be disabled.");

        user.SetActive(command.IsActive);
        await _users.SaveAsync(ct);
        return new TeamMemberDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.IsActive, user.CreatedAt);
    }
}
