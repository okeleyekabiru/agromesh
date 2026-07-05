namespace AgroMesh.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string OwnerId { get; private set; } = string.Empty;
    public bool IsDefaultCompany { get; private set; }

    public List<Farm> Farms { get; private set; } = [];

    private Company() { }

    public static Company Create(string name, string ownerId, bool isDefaultCompany = true) => new()
    {
        Name = name,
        OwnerId = ownerId,
        IsDefaultCompany = isDefaultCompany
    };

    public void Rename(string name)
    {
        Name = name;
        UpdatedAt = DateTime.UtcNow;
    }
}
