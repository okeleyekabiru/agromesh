using Ilevora.Application.Commands.Staff;
using Ilevora.Application.DTOs;
using Ilevora.Application.Email;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.Utilities;
using MediatR;

namespace Ilevora.Application.Commands.Invitations;

internal static class InvitationRules
{
    public static readonly TimeSpan Ttl = TimeSpan.FromDays(7);
    public const string InvalidLink = "This invitation link is invalid or has expired.";

    public static string RoleLabel(UserRole r) => r switch { UserRole.FarmManager => "Farm manager", UserRole.Supervisor => "Supervisor", _ => r.ToString() };

    public static InvitationDto ToDto(Invitation i, DateTime now) =>
        new(i.Id, i.Email, i.Role.ToString(), i.StatusAt(now).ToString(), i.CreatedAt, i.ExpiresAt);

    public static Task Notify(IEmailSender email, IAppUrls urls, string to, string inviterName, string farmName, UserRole role, string rawToken, CancellationToken ct) =>
        email.TrySendAsync(EmailTemplates.Invitation(to, inviterName, farmName, RoleLabel(role), urls.AcceptInvite(rawToken), (int)Ttl.TotalDays), ct);
}

// ---- create --------------------------------------------------------------------------------------------------------

public record CreateInvitationCommand(CreateInvitationRequest Request) : IRequest<InvitationDto>;

public class CreateInvitationCommandHandler : IRequestHandler<CreateInvitationCommand, InvitationDto>
{
    private readonly IInvitationRepository _invites;
    private readonly IUserRepository _users;
    private readonly ICompanyRepository _companies;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IEmailSender _email;
    private readonly IAppUrls _urls;
    private readonly TimeProvider _clock;

    public CreateInvitationCommandHandler(IInvitationRepository invites, IUserRepository users, ICompanyRepository companies,
        ICurrentUserAccessor currentUser, IEmailSender email, IAppUrls urls, TimeProvider clock)
    {
        _invites = invites; _users = users; _companies = companies; _currentUser = currentUser; _email = email; _urls = urls; _clock = clock;
    }

    public async Task<InvitationDto> Handle(CreateInvitationCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var now = _clock.GetUtcNow().UtcDateTime;

        if (!Enum.TryParse<UserRole>(req.Role, ignoreCase: true, out var role) || role is not (UserRole.FarmManager or UserRole.Supervisor))
            throw new DomainException("Role must be 'FarmManager' or 'Supervisor'.");
        if (!AddStaffUserCommandHandler.CanAdd(_currentUser.Role, role))
            throw new ForbiddenException(role == UserRole.FarmManager ? "Only an admin can invite a farm manager." : "You are not allowed to invite people.");

        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException("Your account is not attached to a company.");
        var email = (req.Email ?? "").Trim().ToLowerInvariant();
        if (email.Length == 0 || email.Length > 200 || !EmailAddress.IsPlausible(email))
            throw new DomainException("A valid email is required.");

        if (await _users.GetByEmailAsync(email, ct) is not null)
            throw new DomainException("An account with this email already exists.");
        var existing = await _invites.GetByCompanyAsync(companyId, ct);
        if (existing.Any(i => i.Email == email && i.StatusAt(now) == InvitationStatus.Pending))
            throw new DomainException("This email already has a pending invitation. Resend it instead.");

        var company = await _companies.GetByIdAsync(companyId, ct) ?? throw new ForbiddenException("Your account is not attached to a company.");
        var inviter = _currentUser.UserId is null ? null : await _users.GetByIdAsync(_currentUser.UserId, ct);

        var raw = SecureToken.Generate();
        var invite = Invitation.Create(companyId, email, role, _currentUser.UserId ?? "", SecureToken.Hash(raw), now, InvitationRules.Ttl);
        await _invites.AddAsync(invite, ct);
        await _invites.SaveAsync(ct);

        await InvitationRules.Notify(_email, _urls, email, inviter?.FullName ?? "Your farm admin", company.Name, role, raw, ct);
        return InvitationRules.ToDto(invite, now);
    }
}

// ---- list ----------------------------------------------------------------------------------------------------------

public record ListInvitationsQuery() : IRequest<List<InvitationDto>>;

public class ListInvitationsQueryHandler : IRequestHandler<ListInvitationsQuery, List<InvitationDto>>
{
    private readonly IInvitationRepository _invites;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly TimeProvider _clock;

    public ListInvitationsQueryHandler(IInvitationRepository invites, ICurrentUserAccessor currentUser, TimeProvider clock)
    {
        _invites = invites; _currentUser = currentUser; _clock = clock;
    }

    public async Task<List<InvitationDto>> Handle(ListInvitationsQuery query, CancellationToken ct)
    {
        if (_currentUser.Role is not (UserRole.Admin or UserRole.FarmManager))
            throw new ForbiddenException("Only an admin or a farm manager can see invitations.");
        var companyId = _currentUser.CompanyId ?? throw new ForbiddenException("Your account is not attached to a company.");
        var now = _clock.GetUtcNow().UtcDateTime;
        return (await _invites.GetByCompanyAsync(companyId, ct))
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => InvitationRules.ToDto(i, now))
            .ToList();
    }
}

// ---- resend / revoke -----------------------------------------------------------------------------------------------

public record ResendInvitationCommand(string Id) : IRequest<InvitationDto>;

public class ResendInvitationCommandHandler : IRequestHandler<ResendInvitationCommand, InvitationDto>
{
    private readonly IInvitationRepository _invites;
    private readonly IUserRepository _users;
    private readonly ICompanyRepository _companies;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly IEmailSender _email;
    private readonly IAppUrls _urls;
    private readonly TimeProvider _clock;

    public ResendInvitationCommandHandler(IInvitationRepository invites, IUserRepository users, ICompanyRepository companies,
        ICurrentUserAccessor currentUser, IEmailSender email, IAppUrls urls, TimeProvider clock)
    {
        _invites = invites; _users = users; _companies = companies; _currentUser = currentUser; _email = email; _urls = urls; _clock = clock;
    }

    public async Task<InvitationDto> Handle(ResendInvitationCommand command, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var invite = await InvitationAccess.LoadManageable(_invites, _currentUser, command.Id, ct);

        if (await _users.GetByEmailAsync(invite.Email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        var raw = SecureToken.Generate();
        invite.Reissue(SecureToken.Hash(raw), now, InvitationRules.Ttl);
        await _invites.SaveAsync(ct);

        var company = await _companies.GetByIdAsync(invite.CompanyId, ct);
        var inviter = _currentUser.UserId is null ? null : await _users.GetByIdAsync(_currentUser.UserId, ct);
        await InvitationRules.Notify(_email, _urls, invite.Email, inviter?.FullName ?? "Your farm admin", company?.Name ?? "your farm", invite.Role, raw, ct);
        return InvitationRules.ToDto(invite, now);
    }
}

public record RevokeInvitationCommand(string Id) : IRequest<InvitationDto>;

public class RevokeInvitationCommandHandler : IRequestHandler<RevokeInvitationCommand, InvitationDto>
{
    private readonly IInvitationRepository _invites;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly TimeProvider _clock;

    public RevokeInvitationCommandHandler(IInvitationRepository invites, ICurrentUserAccessor currentUser, TimeProvider clock)
    {
        _invites = invites; _currentUser = currentUser; _clock = clock;
    }

    public async Task<InvitationDto> Handle(RevokeInvitationCommand command, CancellationToken ct)
    {
        var now = _clock.GetUtcNow().UtcDateTime;
        var invite = await InvitationAccess.LoadManageable(_invites, _currentUser, command.Id, ct);
        invite.Revoke(now);
        await _invites.SaveAsync(ct);
        return InvitationRules.ToDto(invite, now);
    }
}

internal static class InvitationAccess
{
    /// <summary>The invitation, if it belongs to the caller's company and the caller may manage its role. Other companies' ids look like "not found".</summary>
    public static async Task<Invitation> LoadManageable(IInvitationRepository invites, ICurrentUserAccessor user, string id, CancellationToken ct)
    {
        if (user.Role is not (UserRole.Admin or UserRole.FarmManager))
            throw new ForbiddenException("You are not allowed to manage invitations.");
        var invite = await invites.GetByIdAsync(id, ct);
        if (invite is null || user.CompanyId is null || invite.CompanyId != user.CompanyId)
            throw new DomainException("Invitation not found.");
        if (!AddStaffUserCommandHandler.CanAdd(user.Role, invite.Role))
            throw new ForbiddenException("Only an admin can manage a farm manager's invitation.");
        return invite;
    }
}

// ---- preview / accept (anonymous) ----------------------------------------------------------------------------------

internal static class InvitationLookup
{
    /// <summary>Finds a usable invitation by its raw token. Every failure looks the same so a token cannot be probed.</summary>
    public static async Task<Invitation> FindUsable(IInvitationRepository invites, string? token, DateTime now, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 200) throw new DomainException(InvitationRules.InvalidLink);
        var invite = await invites.GetByTokenHashAsync(SecureToken.Hash(token), ct);
        if (invite is null || !SecureToken.Matches(token, invite.TokenHash) || invite.StatusAt(now) != InvitationStatus.Pending)
            throw new DomainException(InvitationRules.InvalidLink);
        return invite;
    }
}

public record GetInvitationPreviewQuery(string Token) : IRequest<InvitationPreviewDto>;

public class GetInvitationPreviewQueryHandler : IRequestHandler<GetInvitationPreviewQuery, InvitationPreviewDto>
{
    private readonly IInvitationRepository _invites;
    private readonly ICompanyRepository _companies;
    private readonly TimeProvider _clock;

    public GetInvitationPreviewQueryHandler(IInvitationRepository invites, ICompanyRepository companies, TimeProvider clock)
    {
        _invites = invites; _companies = companies; _clock = clock;
    }

    public async Task<InvitationPreviewDto> Handle(GetInvitationPreviewQuery query, CancellationToken ct)
    {
        var invite = await InvitationLookup.FindUsable(_invites, query.Token, _clock.GetUtcNow().UtcDateTime, ct);
        var company = await _companies.GetByIdAsync(invite.CompanyId, ct) ?? throw new DomainException(InvitationRules.InvalidLink);
        return new InvitationPreviewDto(invite.Email, company.Name, invite.Role.ToString());
    }
}

public record AcceptInvitationCommand(AcceptInvitationRequest Request) : IRequest<AuthResponse>;

public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, AuthResponse>
{
    private readonly IInvitationRepository _invites;
    private readonly IUserRepository _users;
    private readonly ICompanyRepository _companies;
    private readonly IFarmRepository _farms;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtService _jwt;
    private readonly IEmailSender _email;
    private readonly IAppUrls _urls;
    private readonly TimeProvider _clock;

    public AcceptInvitationCommandHandler(IInvitationRepository invites, IUserRepository users, ICompanyRepository companies,
        IFarmRepository farms, IPasswordHasher hasher, IJwtService jwt, IEmailSender email, IAppUrls urls, TimeProvider clock)
    {
        _invites = invites; _users = users; _companies = companies; _farms = farms; _hasher = hasher; _jwt = jwt; _email = email; _urls = urls; _clock = clock;
    }

    public async Task<AuthResponse> Handle(AcceptInvitationCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var now = _clock.GetUtcNow().UtcDateTime;
        var invite = await InvitationLookup.FindUsable(_invites, req.Token, now, ct);

        if (string.IsNullOrWhiteSpace(req.FullName)) throw new DomainException("Full name is required.");
        if (req.FullName.Trim().Length > 200) throw new DomainException("Full name is too long.");
        if (string.IsNullOrEmpty(req.Password) || req.Password.Length < 8) throw new DomainException("Password must be at least 8 characters.");
        if (await _users.GetByEmailAsync(invite.Email, ct) is not null)
            throw new DomainException("An account with this email already exists.");

        var user = User.CreateStaff(req.FullName.Trim(), invite.Email, _hasher.Hash(req.Password), invite.CompanyId, invite.Role);
        await _users.AddAsync(user, ct);
        invite.Accept(now);
        await _users.SaveAsync(ct);
        await _invites.SaveAsync(ct);

        var farms = await _farms.GetByCompanyIdAsync(invite.CompanyId, ct);
        await _email.TrySendAsync(EmailTemplates.Welcome(user.Email, user.FullName, _urls.Login), ct);

        // Optional: tell the inviter, if they are still active and have not opted out of optional email.
        var inviter = await _users.GetByIdAsync(invite.InvitedByUserId, ct);
        if (inviter is { IsActive: true, EmailNotificationsEnabled: true })
            await _email.TrySendAsync(EmailTemplates.InvitationAccepted(inviter.Email, inviter.FullName, user.FullName, InvitationRules.RoleLabel(user.Role)), ct);

        return new AuthResponse(
            _jwt.GenerateToken(user),
            new AuthUserDto(user.Id, user.Email, user.FullName, user.CompanyId, user.Role.ToString()),
            user.CompanyId,
            farms.Select(f => new FarmSummaryDto(f.Id, f.Name, f.SetupStatus)).ToList());
    }
}
