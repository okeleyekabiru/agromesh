namespace AgroMesh.Domain.Entities;

public abstract class BaseEntity
{
    public string Id { get; protected set; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; protected set; } = DateTime.UtcNow;
}
