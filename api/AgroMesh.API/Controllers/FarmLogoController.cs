using AgroMesh.API.Filters;
using AgroMesh.Application.Commands.Farm;
using AgroMesh.Application.Queries.Farm;
using AgroMesh.Application.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace AgroMesh.API.Controllers;

/// <summary>
/// The farm's brand logo, shown in place of the AgroMesh logo after login and printed on invoices. Any signed-in user of the
/// company can read it; only an admin or farm manager can change it. Reading needs the bearer token like every other
/// endpoint (an img tag cannot send one, so clients fetch the bytes with their auth header and show a blob/memory image).
/// </summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/logo")]
public class FarmLogoController : ControllerBase
{
    // The logo limit plus room for the multipart envelope. The exact limit is enforced on the file itself.
    private const long RequestLimit = 3 * 1024 * 1024;

    private readonly IMediator _mediator;

    public FarmLogoController(IMediator mediator) => _mediator = mediator;

    /// <summary>Uploads the logo, or replaces the current one. Multipart form with a single "file" part (PNG, JPEG or WebP, up to 2 MB).</summary>
    [HttpPut]
    [RequestSizeLimit(RequestLimit)]
    [UploadSizeLimit(RequestLimit)]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload(string farmId, IFormFile? file, CancellationToken ct)
    {
        if (file is null) return BadRequest(Problem400("Send the logo as a multipart form with a 'file' part."));
        // Size, type and content are checked in the handler after the caller's role, so a caller who may not change the logo gets 403.

        await using var stream = file.OpenReadStream();
        using var buffer = new MemoryStream((int)file.Length);
        await stream.CopyToAsync(buffer, ct);

        return Ok(await _mediator.Send(new UploadFarmLogoCommand(farmId, buffer.ToArray(), file.FileName, file.ContentType), ct));
    }

    /// <summary>The logo image. 404 when the farm has none. Sends an ETag, so a repeat request can be answered with 304.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(string farmId, CancellationToken ct)
    {
        var logo = await _mediator.Send(new GetFarmLogoQuery(farmId), ct);

        Response.Headers[HeaderNames.CacheControl] = "private, max-age=300";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var etag = new EntityTagHeaderValue($"\"{logo.UpdatedAt?.Ticks ?? 0}\"");
        return File(logo.Content, logo.ContentType, lastModified: null, entityTag: etag);
    }

    /// <summary>Removes the logo. Succeeds (204) whether or not there was one.</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete(string farmId, CancellationToken ct)
    {
        await _mediator.Send(new DeleteFarmLogoCommand(farmId), ct);
        return NoContent();
    }

    private ProblemDetails Problem400(string detail) => new()
    {
        Type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        Title = "Bad Request",
        Status = 400,
        Detail = detail,
    };
}
