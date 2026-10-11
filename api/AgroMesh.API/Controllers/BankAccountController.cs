using AgroMesh.Application.Commands.Bank;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

public record LinkBankAccountRequest(string Code);

/// <summary>Linking the farm's bank account through Mono Connect, and pulling its transactions.</summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/bank-account")]
public class BankAccountController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;

    public BankAccountController(IMediator mediator, IConfiguration config)
    {
        _mediator = mediator;
        _config = config;
    }

    /// <summary>Whether a bank is linked, plus the public key the Mono Connect widget needs (the secret key never leaves the server).</summary>
    [HttpGet]
    public async Task<IActionResult> Get(string farmId, CancellationToken ct)
    {
        var s = await _mediator.Send(new GetBankAccountQuery(farmId), ct);
        return Ok(new { s.Linked, s.LinkedAt, s.LastSyncAt, publicKey = _config["Mono:PublicKey"] });
    }

    /// <summary>Called with the one-time code the Mono Connect widget returns.</summary>
    [HttpPost("link")]
    public async Task<IActionResult> Link(string farmId, [FromBody] LinkBankAccountRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new LinkBankAccountCommand(farmId, request.Code), ct));

    [HttpPost("sync")]
    public async Task<IActionResult> Sync(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new SyncBankAccountCommand(farmId), ct));

    [HttpDelete]
    public async Task<IActionResult> Unlink(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new UnlinkBankAccountCommand(farmId), ct));
}
