using AgroMesh.API.Controllers;
using Microsoft.AspNetCore.Authorization;

namespace AgroMesh.Tests;

/// <summary>
/// A plain reflection check — no ASP.NET Core TestServer/WebApplicationFactory precedent exists in
/// this repo to stand up for one feature. Proves two things per Phase 4's confirmed decisions:
/// (1) the new Orders/Suppliers endpoints require an authenticated caller, and (2) that requirement
/// was NOT retroactively bolted onto the pre-existing, still-intentionally-open Phase 1-3 Inventory
/// controllers as a side effect. Checks class-level attributes only — InventoryController's
/// pre-existing method-level [Authorize] on Adjust (Phase 3, unrelated to this feature) is untouched
/// and correctly out of scope for this check.
/// </summary>
public class OrdersAuthorizeAttributeTests
{
    private static bool HasClassLevelAuthorize(Type controllerType) =>
        controllerType.GetCustomAttributes(typeof(AuthorizeAttribute), inherit: false).Length > 0;

    [Fact]
    public void SuppliersController_HasClassLevelAuthorize()
    {
        Assert.True(HasClassLevelAuthorize(typeof(SuppliersController)));
    }

    [Fact]
    public void OrdersController_HasClassLevelAuthorize()
    {
        Assert.True(HasClassLevelAuthorize(typeof(OrdersController)));
    }

    [Fact]
    public void InventoryController_HasNoClassLevelAuthorize()
    {
        // Regression guard: Phase 4 must not retroactively gate this pre-existing, intentionally
        // open controller. Its Adjust action does carry its own method-level [Authorize] from
        // Phase 3 — untouched here, and irrelevant to this class-level check.
        Assert.False(HasClassLevelAuthorize(typeof(InventoryController)));
    }

    [Fact]
    public void InventoryCategoriesController_HasNoClassLevelAuthorize()
    {
        Assert.False(HasClassLevelAuthorize(typeof(InventoryCategoriesController)));
    }

    [Fact]
    public void InventoryAdjustmentsController_StillHasClassLevelAuthorize()
    {
        // Not new — Phase 3 already gated this whole controller. Asserted here too as a companion
        // regression guard: proves Phase 4 didn't accidentally remove it either.
        Assert.True(HasClassLevelAuthorize(typeof(InventoryAdjustmentsController)));
    }
}
