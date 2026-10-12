using AgroMesh.Application.Commands.Inventory;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Inventory;
using AgroMesh.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

/// <summary>The approval queue for stock prices: a price asked for on the Inventory page waits here for an admin.</summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/inventory-price-changes")]
public class InventoryPriceChangesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InventoryPriceChangesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, [FromQuery] PriceChangeStatus? status, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetPriceChangeRequestsQuery(farmId, status), ct));

    [HttpPatch("{requestId}/approve")]
    public async Task<IActionResult> Approve(string farmId, string requestId, [FromBody] ReviewPriceChangeRequest? request, CancellationToken ct) =>
        Ok(await _mediator.Send(new ApprovePriceChangeCommand(farmId, requestId, request?.ReviewNotes), ct));

    [HttpPatch("{requestId}/reject")]
    public async Task<IActionResult> Reject(string farmId, string requestId, [FromBody] ReviewPriceChangeRequest? request, CancellationToken ct) =>
        Ok(await _mediator.Send(new RejectPriceChangeCommand(farmId, requestId, request?.ReviewNotes), ct));
}
