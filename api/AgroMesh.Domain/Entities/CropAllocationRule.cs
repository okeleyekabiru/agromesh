using AgroMesh.Domain.Enums;

namespace AgroMesh.Domain.Entities;

public class CropAllocationRule : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public TransactionCategory Category { get; private set; }
    public string? BlockId { get; private set; }
    public List<int>? SeasonMonths { get; private set; }
    public Dictionary<string, double> Allocation { get; private set; } = [];

    private CropAllocationRule() { }

    public static CropAllocationRule Create(
        string farmId,
        TransactionCategory category,
        Dictionary<string, double> allocation,
        string? blockId = null,
        List<int>? seasonMonths = null)
    {
        return new CropAllocationRule
        {
            FarmId = farmId,
            Category = category,
            Allocation = allocation,
            BlockId = blockId,
            SeasonMonths = seasonMonths
        };
    }

    public void Update(Dictionary<string, double> allocation, List<int>? seasonMonths)
    {
        Allocation = allocation;
        SeasonMonths = seasonMonths;
        UpdatedAt = DateTime.UtcNow;
    }
}
