using Ilevora.Domain.Enums;

namespace Ilevora.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? CompanyId { get; private set; }

    // RegisterCommand is the only path that creates a User, and the registrant is always the sole
    // owner of their own brand-new Company/Farm (no invite/multi-user-per-company flow exists in
    // this codebase) — so every user bootstraps as Admin of everything they can see.
    public UserRole Role { get; private set; } = UserRole.Admin;

    // A disabled account cannot sign in, and a token it already holds stops working. Only an admin changes this.
    public bool IsActive { get; private set; } = true;

    // Bumped whenever the password changes. Tokens carry the version they were issued with, so a reset or change
    // signs out every session issued before it.
    public int TokenVersion { get; private set; }

    // Optional email (e.g. "your invitee joined") only. Security and account email (invite, reset, password changed) is always sent.
    public bool EmailNotificationsEnabled { get; private set; } = true;

    private User() { }

    public bool IsTokenCurrent(int? tokenVersionClaim) => (tokenVersionClaim ?? 0) == TokenVersion;

    public void SetEmailNotifications(bool enabled)
    {
        EmailNotificationsEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public static User Create(string fullName, string email, string passwordHash) => new()
    {
        FullName = fullName,
        Email = email.Trim().ToLowerInvariant(),
        PasswordHash = passwordHash,
        Role = UserRole.Admin,
    };

    /// <summary>A staff account added by an existing user to their own company.</summary>
    public static User CreateStaff(string fullName, string email, string passwordHash, string companyId, UserRole role) => new()
    {
        FullName = fullName,
        Email = email.Trim().ToLowerInvariant(),
        PasswordHash = passwordHash,
        CompanyId = companyId,
        Role = role,
    };

    public void SetActive(bool active)
    {
        IsActive = active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordHash(string passwordHash)
    {
        PasswordHash = passwordHash;
        TokenVersion++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignCompany(string companyId)
    {
        CompanyId = companyId;
        UpdatedAt = DateTime.UtcNow;
    }
}
