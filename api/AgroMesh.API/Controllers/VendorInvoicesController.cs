using System.ComponentModel.DataAnnotations;
using AgroMesh.API.Filters;
using AgroMesh.Application.Commands.VendorInvoice;
using AgroMesh.Application.DTOs;
using AgroMesh.Application.Queries.VendorInvoice;
using AgroMesh.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace AgroMesh.API.Controllers;

/// <summary>The multipart form for attaching a vendor invoice: the file plus its details.</summary>
public class UploadVendorInvoiceForm
{
    [Required] public IFormFile? File { get; set; }
    public string? VendorName { get; set; }
    [Required] public string? InvoiceNumber { get; set; }
    /// <summary>Digits with an optional single "." fraction, e.g. 5000.50. Anything else (1,5  1e3  " 12 ") is a 400, never a guess.</summary>
    [Required] public string? Amount { get; set; }
    [Required] public DateOnly? InvoiceDate { get; set; }
    public DateOnly? DueDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Invoices that vendors (suppliers) issued to the farm, attached to completed (Received) orders. Read and download: any user of
/// the company. Upload: admin, farm manager or supervisor. Mark paid/unpaid and delete: admin or farm manager.
/// </summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}")]
public class VendorInvoicesController : ControllerBase
{
    // The file limit plus room for the form fields and multipart envelope. The exact limit is enforced on the file itself.
    private const long RequestLimit = 11 * 1024 * 1024;

    private readonly IMediator _mediator;

    public VendorInvoicesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Attaches a vendor invoice to a Received order. Multipart form: file (PDF, PNG or JPEG, up to 10 MB) plus the fields of <see cref="UploadVendorInvoiceForm"/>.</summary>
    [HttpPost("orders/{orderId}/vendor-invoices")]
    [RequestSizeLimit(RequestLimit)]
    [UploadSizeLimit(RequestLimit)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(string farmId, string orderId, [FromForm] UploadVendorInvoiceForm form, CancellationToken ct)
    {
        var file = form.File!;
        if (!StrictDecimal.TryParse(form.Amount, out var amount))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["Amount"] = ["Amount must be digits with an optional '.' decimal part, for example 5000.50."] })
            {
                Status = 400,
                Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
            });

        // The file is checked (size, type, content) inside the handler, after the caller's role, so a caller who may not upload gets 403.

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream((int)file.Length);
        await stream.CopyToAsync(buffer, ct);

        var fields = new UploadVendorInvoiceFields(form.VendorName, form.InvoiceNumber!, amount, form.InvoiceDate!.Value, form.DueDate, form.Notes);
        var result = await _mediator.Send(new UploadVendorInvoiceCommand(farmId, orderId, buffer.ToArray(), file.FileName, file.ContentType, fields), ct);
        return Created($"api/farms/{farmId}/vendor-invoices/{result.Id}", result);
    }

    /// <summary>The vendor invoices attached to one order, newest first.</summary>
    [HttpGet("orders/{orderId}/vendor-invoices")]
    public async Task<IActionResult> GetForOrder(string farmId, string orderId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetVendorInvoicesQuery(farmId, orderId, null), ct));

    /// <summary>Every vendor invoice of the farm, newest first. Optional filters: status (Paid/Unpaid) and orderId.</summary>
    [HttpGet("vendor-invoices")]
    public async Task<IActionResult> GetAll(string farmId, [FromQuery] string? status, [FromQuery] string? orderId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetVendorInvoicesQuery(farmId, orderId, status), ct));

    [HttpGet("vendor-invoices/{vendorInvoiceId}")]
    public async Task<IActionResult> GetById(string farmId, string vendorInvoiceId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetVendorInvoiceQuery(farmId, vendorInvoiceId), ct));

    /// <summary>The uploaded file. A download by default; with inline=true it is sent for display in the browser or app viewer.</summary>
    [HttpGet("vendor-invoices/{vendorInvoiceId}/file")]
    public async Task<IActionResult> Download(string farmId, string vendorInvoiceId, [FromQuery] bool inline, CancellationToken ct)
    {
        var file = await _mediator.Send(new DownloadVendorInvoiceFileQuery(farmId, vendorInvoiceId), ct);

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        if (!inline) return File(file.Content, file.ContentType, file.FileName);

        Response.Headers[HeaderNames.ContentDisposition] = new ContentDispositionHeaderValue("inline") { FileNameStar = file.FileName }.ToString();
        return File(file.Content, file.ContentType);
    }

    [HttpPatch("vendor-invoices/{vendorInvoiceId}/status")]
    public async Task<IActionResult> SetStatus(string farmId, string vendorInvoiceId, [FromBody] SetVendorInvoiceStatusRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new SetVendorInvoiceStatusCommand(farmId, vendorInvoiceId, request), ct));

    [HttpDelete("vendor-invoices/{vendorInvoiceId}")]
    public async Task<IActionResult> Delete(string farmId, string vendorInvoiceId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteVendorInvoiceCommand(farmId, vendorInvoiceId), ct);
        return NoContent();
    }
}
