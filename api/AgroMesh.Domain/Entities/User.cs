namespace AgroMesh.Domain.Entities;

public class User : BaseEntity
{
    public string FullName { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string? FarmId { get; private set; }

    private User() { }

    public static User Create(string fullName, string email, string passwordHash) => new()
    {
        FullName = fullName,
        Email = email.Trim().ToLowerInvariant(),
        PasswordHash = passwordHash,
    };

    public void AssignFarm(string farmId)
    {
        FarmId = farmId;
        UpdatedAt = DateTime.UtcNow;
    }
}
