using Ilevora.Application.Interfaces;
using Ilevora.Domain.Exceptions;
using MediatR;

namespace Ilevora.Application.Commands.Staff;

public record EmailPreferenceDto(bool Enabled);

public record SetEmailPreferenceRequest(bool Enabled);

/// <summary>The caller's own setting for optional email. Invite, reset and password-changed email are always sent.</summary>
public record GetEmailPreferenceQuery() : IRequest<EmailPreferenceDto>;

public class GetEmailPreferenceQueryHandler : IRequestHandler<GetEmailPreferenceQuery, EmailPreferenceDto>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserAccessor _currentUser;

    public GetEmailPreferenceQueryHandler(IUserRepository users, ICurrentUserAccessor currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<EmailPreferenceDto> Handle(GetEmailPreferenceQuery query, CancellationToken ct)
    {
        var user = _currentUser.UserId is null ? null : await _users.GetByIdAsync(_currentUser.UserId, ct);
        if (user is null) throw new ForbiddenException("Sign in to change email settings.");
        return new EmailPreferenceDto(user.EmailNotificationsEnabled);
    }
}

public record SetEmailPreferenceCommand(bool Enabled) : IRequest<EmailPreferenceDto>;

public class SetEmailPreferenceCommandHandler : IRequestHandler<SetEmailPreferenceCommand, EmailPreferenceDto>
{
    private readonly IUserRepository _users;
    private readonly ICurrentUserAccessor _currentUser;

    public SetEmailPreferenceCommandHandler(IUserRepository users, ICurrentUserAccessor currentUser)
    {
        _users = users;
        _currentUser = currentUser;
    }

    public async Task<EmailPreferenceDto> Handle(SetEmailPreferenceCommand command, CancellationToken ct)
    {
        var user = _currentUser.UserId is null ? null : await _users.GetByIdAsync(_currentUser.UserId, ct);
        if (user is null) throw new ForbiddenException("Sign in to change email settings.");
        user.SetEmailNotifications(command.Enabled);
        await _users.SaveAsync(ct);
        return new EmailPreferenceDto(user.EmailNotificationsEnabled);
    }
}
