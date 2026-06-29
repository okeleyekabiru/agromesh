namespace AgroMesh.Domain.Entities;

public class Block : BaseEntity
{
    public string SectionId { get; private set; } = string.Empty;
    public string FarmId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public double Hectares { get; private set; }
    public int? TreeCount { get; private set; }

    private Block() { }

    public static Block Create(string sectionId, string farmId, string name, double hectares, int? treeCount = null)
    {
        return new Block
        {
            SectionId = sectionId,
            FarmId = farmId,
            Name = name,
            Hectares = hectares,
            TreeCount = treeCount
        };
    }

    public void Update(string name, double hectares, int? treeCount)
    {
        Name = name;
        Hectares = hectares;
        TreeCount = treeCount;
        UpdatedAt = DateTime.UtcNow;
    }
}
