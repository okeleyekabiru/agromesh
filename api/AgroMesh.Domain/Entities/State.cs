namespace AgroMesh.Domain.Entities;

public class State : BaseEntity
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;

    private State() { }

    public static State Create(string name, string code) => new()
    {
        Name = name,
        Code = code
    };
}
