using AgroMesh.Application.Commands.Transaction;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Transaction;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

[ApiController]
[Authorize]
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

    /// <summary>Income, capital and expenses for a period (defaults to this month so far).</summary>
    [HttpGet("summary")]
    public async Task<IActionResult> Summary(string farmId, [FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetFinanceSummaryQuery(farmId, from, to), ct));

    /// <summary>Transfers that named an order but did not match cleanly: a person confirms or dismisses each.</summary>
    [HttpGet("needs-review")]
    public async Task<IActionResult> NeedsReview(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetReconciliationQueueQuery(farmId), ct));

    [HttpPost("{transactionId}/reconcile")]
    public async Task<IActionResult> Reconcile(string farmId, string transactionId, [FromBody] ResolveReconciliationRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new ResolveReconciliationCommand(farmId, transactionId, request), ct));

    /// <summary>Move money in between Income and Capital.</summary>
    [HttpPatch("{transactionId}/kind")]
    public async Task<IActionResult> SetKind(string farmId, string transactionId, [FromBody] SetTransactionKindRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new SetTransactionKindCommand(farmId, transactionId, request.Kind), ct));

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
}
