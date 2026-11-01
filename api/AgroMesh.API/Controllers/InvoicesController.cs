using AgroMesh.Application.Commands.Invoice;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.Invoice;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

/// <summary>
/// Invoices the farm issues to its customers. Read (list, get, templates, pdf): any user of the company. Write (create, update,
/// delete): admin or farm manager. Not to be confused with the vendor invoices a supplier sends the farm (VendorInvoicesController).
/// </summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvoicesController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetInvoicesQuery(farmId), ct));

    [HttpPost]
    public async Task<IActionResult> Create(string farmId, [FromBody] SaveInvoiceRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateInvoiceCommand(farmId, request), ct);
        return Created($"api/farms/{farmId}/invoices/{result.Id}", result);
    }

    [HttpGet("{invoiceId}")]
    public async Task<IActionResult> GetById(string farmId, string invoiceId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetInvoiceQuery(farmId, invoiceId), ct));

    [HttpPut("{invoiceId}")]
    public async Task<IActionResult> Update(string farmId, string invoiceId, [FromBody] SaveInvoiceRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new UpdateInvoiceCommand(farmId, invoiceId, request), ct));

    [HttpDelete("{invoiceId}")]
    public async Task<IActionResult> Delete(string farmId, string invoiceId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteInvoiceCommand(farmId, invoiceId), ct);
        return NoContent();
    }

    /// <summary>The invoice as an A4 PDF in its template, with the farm logo. Sent as a download named after the invoice number.</summary>
    [HttpGet("{invoiceId}/pdf")]
    public async Task<IActionResult> GetPdf(string farmId, string invoiceId, CancellationToken ct)
    {
        var pdf = await _mediator.Send(new GenerateInvoicePdfQuery(farmId, invoiceId), ct);
        return File(pdf.Content, "application/pdf", pdf.FileName);
    }

    /// <summary>The invoice layouts the farm can choose from. Pass a template's key as templateKey when saving an invoice.</summary>
    [HttpGet("~/api/farms/{farmId}/invoice-templates")]
    public async Task<IActionResult> GetTemplates(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetInvoiceTemplatesQuery(farmId), ct));
}
