using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Inventory;
using AgroMesh.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

/// <summary>
/// The approval queue for Phase 3 stock adjustments — sibling-route pattern to
/// InventoryCategoriesController. [Authorize] at the controller level: every action here either
/// needs to know who's calling (approve/reject record ReviewedByUserId + check role) or is part of
/// the same identity-dependent workflow (the queue listing).
/// </summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/inventory-adjustments")]
public class InventoryAdjustmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryAdjustmentsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, [FromQuery] StockAdjustmentStatus? status, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetStockAdjustmentRequestsQuery(farmId, status), ct));

    [HttpPatch("{requestId}/approve")]
    public async Task<IActionResult> Approve(string farmId, string requestId, [FromBody] ReviewStockAdjustmentRequest? request, CancellationToken ct) =>
        Ok(await _mediator.Send(new ApproveStockAdjustmentCommand(farmId, requestId, request?.ReviewNotes), ct));

    [HttpPatch("{requestId}/reject")]
    public async Task<IActionResult> Reject(string farmId, string requestId, [FromBody] ReviewStockAdjustmentRequest? request, CancellationToken ct) =>
        Ok(await _mediator.Send(new RejectStockAdjustmentCommand(farmId, requestId, request?.ReviewNotes), ct));
}
