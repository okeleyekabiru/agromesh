using AgroMesh.Application.Commands.Transaction;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Transaction;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Route("api/farms/{farmId}/transactions")]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(
        string farmId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] bool? pendingReview = null,
        CancellationToken ct = default) =>
        Ok(await _mediator.Send(new GetTransactionsQuery(farmId, page, pageSize, pendingReview), ct));

    [HttpPatch("{transactionId}/review")]
    public async Task<IActionResult> Review(string transactionId, [FromBody] ReviewTransactionRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new ReviewTransactionCommand(transactionId, request), ct);
        return Ok(result);
    }

    [HttpPost("{transactionId}/inventory-receipt")]
    public async Task<IActionResult> RecordReceipt(string transactionId, [FromBody] RecordInventoryReceiptRequest request, CancellationToken ct)
    {
        await _mediator.Send(new RecordInventoryReceiptCommand(transactionId, request.InventoryItemId, request.Quantity), ct);
        return NoContent();
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(string farmId, [FromQuery] string monoAccountId, CancellationToken ct)
    {
        var count = await _mediator.Send(new SyncTransactionsCommand(farmId, monoAccountId), ct);
        return Ok(new { synced = count });
    }
}
