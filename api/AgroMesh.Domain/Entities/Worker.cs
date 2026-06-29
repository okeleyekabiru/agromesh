using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class Worker : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string Initials { get; private set; } = string.Empty;
    public string WorkerRoleId { get; private set; } = string.Empty;
    public WorkerType Type { get; private set; }
    public bool Active { get; private set; } = true;

    public WorkerRole Role { get; private set; } = null!;

    private Worker() { }

    public static Worker Create(string farmId, string name, string initials, string workerRoleId, WorkerType type)
    {
        return new Worker
        {
            FarmId = farmId,
            Name = name,
            Initials = initials,
            WorkerRoleId = workerRoleId,
            Type = type
        };
    }

    public void Deactivate()
    {
        Active = false;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Reactivate()
    {
        Active = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateRole(string workerRoleId, WorkerType type)
    {
        WorkerRoleId = workerRoleId;
        Type = type;
        UpdatedAt = DateTime.UtcNow;
    }
}
