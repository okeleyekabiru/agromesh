using Ilevora.Application.DTOs;
using Ilevora.Application.Email;
using Ilevora.Application.Interfaces;
using Ilevora.Domain.Entities;
using Ilevora.Domain.Exceptions;
using Ilevora.Domain.Utilities;
using MediatR;

namespace Ilevora.Application.Commands.Auth;

internal static class PasswordResetRules
{
    public static readonly TimeSpan Ttl = TimeSpan.FromHours(1);
    public static readonly TimeSpan MinGap = TimeSpan.FromSeconds(60);
    public const string InvalidLink = "This password reset link is invalid or has expired.";
}

/// <summary>
/// Always completes without error, whether or not the email belongs to an account, so the endpoint cannot be used to find out
/// who has an account. Only an existing, active user is mailed, and not more than once a minute.
/// </summary>
public record ForgotPasswordCommand(ForgotPasswordRequest Request) : IRequest;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IEmailSender _email;
    private readonly IAppUrls _urls;
    private readonly TimeProvider _clock;

    public ForgotPasswordCommandHandler(IUserRepository users, IPasswordResetTokenRepository tokens, IEmailSender email, IAppUrls urls, TimeProvider clock)
    {
        _users = users; _tokens = tokens; _email = email; _urls = urls; _clock = clock;
    }

    public async Task Handle(ForgotPasswordCommand command, CancellationToken ct)
    {
        var email = command.Request.Email?.Trim();
        if (string.IsNullOrEmpty(email) || email.Length > 320) return;

        var user = await _users.GetByEmailAsync(email, ct);
        if (user is null || !user.IsActive) return;

        var now = _clock.GetUtcNow().UtcDateTime;
        var recent = (await _tokens.GetByUserAsync(user.Id, ct)).Any(t => now - t.CreatedAt < PasswordResetRules.MinGap);
        if (recent) return;

        var raw = SecureToken.Generate();
        await _tokens.AddAsync(PasswordResetToken.Create(user.Id, SecureToken.Hash(raw), now, PasswordResetRules.Ttl), ct);
        await _tokens.SaveAsync(ct);

        await _email.TrySendAsync(EmailTemplates.PasswordReset(user.Email, user.FullName, _urls.ResetPassword(raw), (int)PasswordResetRules.Ttl.TotalMinutes), ct);
    }
}

public record ResetPasswordCommand(ResetPasswordRequest Request) : IRequest;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IUserRepository _users;
    private readonly IPasswordResetTokenRepository _tokens;
    private readonly IPasswordHasher _hasher;
    private readonly IEmailSender _email;
    private readonly IAppUrls _urls;
    private readonly TimeProvider _clock;

    public ResetPasswordCommandHandler(IUserRepository users, IPasswordResetTokenRepository tokens, IPasswordHasher hasher, IEmailSender email, IAppUrls urls, TimeProvider clock)
    {
        _users = users; _tokens = tokens; _hasher = hasher; _email = email; _urls = urls; _clock = clock;
    }

    public async Task Handle(ResetPasswordCommand command, CancellationToken ct)
    {
        var req = command.Request;
        var now = _clock.GetUtcNow().UtcDateTime;

        if (string.IsNullOrWhiteSpace(req.Token) || req.Token.Length > 200) throw new DomainException(PasswordResetRules.InvalidLink);
        var token = await _tokens.GetByTokenHashAsync(SecureToken.Hash(req.Token), ct);
        if (token is null || !SecureToken.Matches(req.Token, token.TokenHash) || !token.IsUsable(now))
            throw new DomainException(PasswordResetRules.InvalidLink);

        if (string.IsNullOrEmpty(req.NewPassword) || req.NewPassword.Length < 8)
            throw new DomainException("Password must be at least 8 characters.");

        var user = await _users.GetByIdAsync(token.UserId, ct) ?? throw new DomainException(PasswordResetRules.InvalidLink);
        if (!user.IsActive) throw new DomainException(PasswordResetRules.InvalidLink);

        user.SetPasswordHash(_hasher.Hash(req.NewPassword));
        foreach (var t in await _tokens.GetByUserAsync(user.Id, ct))
            if (t.IsUsable(now)) t.MarkUsed(now);
        await _users.SaveAsync(ct);
        await _tokens.SaveAsync(ct);

        await _email.TrySendAsync(EmailTemplates.PasswordChanged(user.Email, user.FullName), ct);
    }
}
