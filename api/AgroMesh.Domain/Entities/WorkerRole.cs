using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class WorkerRole : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public WorkerType DefaultType { get; private set; }
    public decimal DailyRate { get; private set; }

    public List<Worker> Workers { get; private set; } = [];

    private WorkerRole() { }

    public static WorkerRole Create(string farmId, string title, WorkerType defaultType, decimal dailyRate)
    {
        return new WorkerRole
        {
            FarmId = farmId,
            Title = title,
            DefaultType = defaultType,
            DailyRate = dailyRate
        };
    }

    public void UpdateRate(decimal newRate)
    {
        DailyRate = newRate;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Update(string title, WorkerType defaultType, decimal dailyRate)
    {
        Title = title;
        DefaultType = defaultType;
        DailyRate = dailyRate;
        UpdatedAt = DateTime.UtcNow;
    }
}
