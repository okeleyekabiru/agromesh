using AgroMesh.Domain.Entities;
using AgroMesh.Domain.Enums;

namespace AgroMesh.Tests;

public class UserTests
{
    // RegisterCommand is the only path that creates a User today, and the registrant is always the
    // sole owner of their own brand-new Company/Farm (no invite/multi-user-per-company flow exists
    // anywhere in this codebase) — so defaulting to Admin here is a safe bootstrap, not a privilege
    // escalation risk. See docs/features/inventory-adjustment-approval/spec.md, Design decision 4.
    [Fact]
    public void Create_DefaultsRoleToAdmin()
    {
        var user = User.Create("Kabiru Okeleye", "kabiru@example.com", "hash");
        Assert.Equal(UserRole.Admin, user.Role);
    }
}
