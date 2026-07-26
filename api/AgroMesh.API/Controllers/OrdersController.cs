using AgroMesh.Application.Commands.Order;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Order;
using AgroMesh.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

/// <summary>
/// [Authorize] at the controller level per Phase 4's confirmed decision — any authenticated user,
/// no admin/role gate (placing/receiving an order is not the same kind of sensitive action as
/// Phase 3's stock-adjustment approval). See SuppliersController's doc comment for the same
/// reasoning; this does not retroactively gate the pre-existing Inventory* controllers.
/// </summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/orders")]
public class OrdersController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrdersController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, [FromQuery] OrderStatus? status, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetOrdersQuery(farmId, status), ct));

    [HttpPost]
    public async Task<IActionResult> Create(string farmId, [FromBody] CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateOrderCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/orders/{result.Id}", result);
    }

    [HttpGet("{orderId}")]
    public async Task<IActionResult> GetById(string farmId, string orderId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetOrderByIdQuery(farmId, orderId), ct));

    [HttpPatch("{orderId}/place")]
    public async Task<IActionResult> Place(string farmId, string orderId, CancellationToken ct) =>
        Ok(await _mediator.Send(new PlaceOrderCommand(farmId, orderId), ct));

    [HttpPatch("{orderId}/receive")]
    public async Task<IActionResult> Receive(string farmId, string orderId, CancellationToken ct) =>
        Ok(await _mediator.Send(new ReceiveOrderCommand(farmId, orderId), ct));

    [HttpGet("{orderId}/invoice")]
    public async Task<IActionResult> GetInvoice(string farmId, string orderId, CancellationToken ct)
    {
        var bytes = await _mediator.Send(new GenerateOrderInvoiceQuery(farmId, orderId), ct);
        return File(bytes, "application/pdf", $"invoice-{orderId}.pdf");
    }
}
