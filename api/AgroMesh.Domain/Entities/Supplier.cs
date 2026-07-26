using AgroMesh.Domain.Exceptions;

namespace AgroMesh.Domain.Entities;

/// <summary>
/// A farm-scoped supplier an Order can be placed with. Mirrors InventoryCategory's shape
/// (FarmId, Name, private setters, static Create) — see that entity's doc comment for the
/// established farm-scoped-entity convention this follows.
/// </summary>
public class Supplier : BaseEntity
{
    public string FarmId { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Phone { get; private set; }
    public string? Email { get; private set; }

    private Supplier() { }

    public static Supplier Create(string farmId, string name, string? phone = null, string? email = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Supplier name is required.");

        return new Supplier
        {
            FarmId = farmId,
            Name = name.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim()
        };
    }
}
