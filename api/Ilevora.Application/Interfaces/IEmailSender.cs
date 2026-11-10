namespace Ilevora.Application.Interfaces;

public record EmailMessage(string To, string Subject, string Html, string Text);

/// <summary>
/// Sends a transactional email. Implementations must not block the caller on the network and should not throw for delivery
/// problems; callers still go through <see cref="EmailSenderExtensions.TrySendAsync"/> so a misbehaving sender can never fail a request.
/// </summary>
public interface IEmailSender
{
    Task SendAsync(EmailMessage message, CancellationToken ct = default);
}

public static class EmailSenderExtensions
{
    /// <summary>Sends and swallows any failure: the user's action must not depend on email.</summary>
    public static async Task TrySendAsync(this IEmailSender sender, EmailMessage message, CancellationToken ct = default)
    {
        try { await sender.SendAsync(message, ct); }
        catch { /* the sender logs its own failures; never break the user's action */ }
    }
}

/// <summary>Builds the web links that go into emails.</summary>
public interface IAppUrls
{
    string AcceptInvite(string token);
    string ResetPassword(string token);
    string Login { get; }
}
