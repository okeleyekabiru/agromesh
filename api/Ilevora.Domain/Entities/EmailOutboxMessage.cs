namespace Ilevora.Domain.Entities;

public enum EmailOutboxStatus { Pending, Sending, Sent, Failed }

/// <summary>
/// An email waiting to be delivered. Rows are claimed with a lease, so a crashed instance's message is picked up again later
/// (delivery is at-least-once) and several instances never send the same row at once. The body is cleared as soon as the row is
/// finished, so one-time links do not linger in the database.
/// </summary>
public class EmailOutboxMessage : BaseEntity
{
    public const int MaxAttempts = 5;
    private static readonly TimeSpan[] Backoff = [TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(15), TimeSpan.FromHours(1)];

    public string To { get; private set; } = string.Empty;
    public string Subject { get; private set; } = string.Empty;
    public string Html { get; private set; } = string.Empty;
    public string Text { get; private set; } = string.Empty;
    public EmailOutboxStatus Status { get; private set; }
    public int Attempts { get; private set; }
    public DateTime NextAttemptAt { get; private set; }
    public DateTime? LeaseUntil { get; private set; }
    public string? ClaimToken { get; private set; }
    /// <summary>Error type only (never a response body, key or token).</summary>
    public string? LastError { get; private set; }
    public DateTime? SentAt { get; private set; }

    private EmailOutboxMessage() { }

    public static EmailOutboxMessage Create(string to, string subject, string html, string text, DateTime now) => new()
    {
        To = to, Subject = subject, Html = html, Text = text,
        Status = EmailOutboxStatus.Pending, NextAttemptAt = now, CreatedAt = now, UpdatedAt = now,
    };

    public void RecordClaim(DateTime now, TimeSpan lease)
    {
        Status = EmailOutboxStatus.Sending;
        Attempts++;
        LeaseUntil = now + lease;
        UpdatedAt = now;
    }

    public void MarkSent(DateTime now)
    {
        Status = EmailOutboxStatus.Sent;
        SentAt = now;
        LeaseUntil = null;
        ClaimToken = null;
        LastError = null;
        ClearBody();
        UpdatedAt = now;
    }

    public void RecordFailure(string error, DateTime now)
    {
        LastError = error.Length > 200 ? error[..200] : error;
        LeaseUntil = null;
        ClaimToken = null;
        UpdatedAt = now;
        if (Attempts >= MaxAttempts)
        {
            Status = EmailOutboxStatus.Failed;
            ClearBody();
            return;
        }
        Status = EmailOutboxStatus.Pending;
        NextAttemptAt = now + Backoff[Math.Min(Math.Max(Attempts, 1), Backoff.Length) - 1];
    }

    private void ClearBody() { Html = string.Empty; Text = string.Empty; }
}
