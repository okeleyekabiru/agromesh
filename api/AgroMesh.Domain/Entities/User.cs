using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

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

    private User() { }

    public static User Create(string fullName, string email, string passwordHash) => new()
    {
        FullName = fullName,
        Email = email.Trim().ToLowerInvariant(),
        PasswordHash = passwordHash,
        Role = UserRole.Admin,
    };

    public void AssignCompany(string companyId)
    {
        CompanyId = companyId;
        UpdatedAt = DateTime.UtcNow;
    }
}
