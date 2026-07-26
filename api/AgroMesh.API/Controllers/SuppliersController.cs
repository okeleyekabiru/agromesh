using AgroMesh.Application.Commands.Supplier;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Supplier;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

/// <summary>
/// Sibling-route pattern to InventoryCategoriesController, but [Authorize] at the controller level
/// per Phase 4's confirmed decision: new Orders/Suppliers endpoints require any authenticated user
/// (not admin-only, unlike Phase 3's approve/reject) — a deliberate, scoped choice that does NOT
/// retroactively gate the still-open Phase 1-3 Inventory* endpoints.
/// </summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/suppliers")]
public class SuppliersController : ControllerBase
{
    private readonly IMediator _mediator;

    public SuppliersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetSuppliersQuery(farmId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(string farmId, [FromBody] CreateSupplierRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateSupplierCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/suppliers/{result.Id}", result);
    }
}
