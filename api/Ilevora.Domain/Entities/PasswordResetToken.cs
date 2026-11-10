namespace Ilevora.Domain.Entities;

/// <summary>A single-use, short-lived password reset link. Only the token's hash is stored.</summary>
public class PasswordResetToken : BaseEntity
{
    public string UserId { get; private set; } = string.Empty;
    public string TokenHash { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? UsedAt { get; private set; }

    private PasswordResetToken() { }

    public static PasswordResetToken Create(string userId, string tokenHash, DateTime now, TimeSpan ttl) => new()
    {
        UserId = userId,
        TokenHash = tokenHash,
        ExpiresAt = now + ttl,
        CreatedAt = now,
        UpdatedAt = now,
    };

    public bool IsUsable(DateTime now) => UsedAt is null && now <= ExpiresAt;

    public void MarkUsed(DateTime now)
    {
        UsedAt = now;
        UpdatedAt = now;
    }
}
