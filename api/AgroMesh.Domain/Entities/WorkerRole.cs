using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class WorkerRole : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public WorkerType DefaultType { get; private set; }
    public decimal PayRate { get; private set; }
    public PayFrequency PayFrequency { get; private set; }

    public List<Worker> Workers { get; private set; } = [];

    private WorkerRole() { }

    public static WorkerRole Create(string farmId, string title, WorkerType defaultType, decimal payRate, PayFrequency payFrequency)
    {
        return new WorkerRole
        {
            FarmId = farmId,
            Title = title,
            DefaultType = defaultType,
            PayRate = payRate,
            PayFrequency = payFrequency
        };
    }

    public void UpdatePay(decimal newRate, PayFrequency newFrequency)
    {
        PayRate = newRate;
        PayFrequency = newFrequency;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string title, WorkerType defaultType, decimal payRate, PayFrequency payFrequency)
    {
        Title = title;
        DefaultType = defaultType;
        PayRate = payRate;
        PayFrequency = payFrequency;
        UpdatedAt = DateTime.UtcNow;
    }
}
