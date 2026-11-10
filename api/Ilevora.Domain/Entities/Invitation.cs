using Ilevora.Domain.Enums;
using Ilevora.Domain.Exceptions;

namespace Ilevora.Domain.Entities;

public enum InvitationStatus { Pending, Accepted, Revoked, Expired }

/// <summary>An offer to join a company with a role, delivered by a single-use expiring link. Only the token's hash is stored.</summary>
public class Invitation : BaseEntity
{
    public string CompanyId { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public string InvitedByUserId { get; private set; } = string.Empty;
    public DateTime ExpiresAt { get; private set; }
    public DateTime? AcceptedAt { get; private set; }
    public DateTime? RevokedAt { get; private set; }

    private Invitation() { }

    public static Invitation Create(string companyId, string email, UserRole role, string invitedByUserId, string tokenHash, DateTime now, TimeSpan ttl) => new()
    {
        CompanyId = companyId,
        Email = email.Trim().ToLowerInvariant(),
        Role = role,
        InvitedByUserId = invitedByUserId,
        TokenHash = tokenHash,
        ExpiresAt = now + ttl,
        CreatedAt = now,
        UpdatedAt = now,
    };

    public InvitationStatus StatusAt(DateTime now)
    {
        if (AcceptedAt is not null) return InvitationStatus.Accepted;
        if (RevokedAt is not null) return InvitationStatus.Revoked;
        return now > ExpiresAt ? InvitationStatus.Expired : InvitationStatus.Pending;
    }

    public void Accept(DateTime now)
    {
        if (StatusAt(now) != InvitationStatus.Pending)
            throw new DomainException("This invitation link is invalid or has expired.");
        AcceptedAt = now;
        UpdatedAt = now;
    }

    public void Revoke(DateTime now)
    {
        if (AcceptedAt is not null) throw new DomainException("This invitation was already accepted.");
        RevokedAt ??= now;
        UpdatedAt = now;
    }

    /// <summary>A new token and a fresh expiry; the previous link stops working because its hash is gone.</summary>
    public void Reissue(string tokenHash, DateTime now, TimeSpan ttl)
    {
        if (AcceptedAt is not null) throw new DomainException("This invitation was already accepted.");
        if (RevokedAt is not null) throw new DomainException("This invitation was revoked. Send a new one.");
        TokenHash = tokenHash;
        ExpiresAt = now + ttl;
        UpdatedAt = now;
    }
}
