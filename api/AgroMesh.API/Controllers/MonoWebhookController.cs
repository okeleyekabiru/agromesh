using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AgroMesh.Application.Commands.Bank;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

/// <summary>
/// Mono calls this when a linked account changes. Mono proves a call is genuine with the secret it generated for
/// the webhook, sent in the "mono-webhook-secret" header. With no secret configured the endpoint refuses every call.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/webhooks/mono")]
public class MonoWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IConfiguration _config;
    private readonly ILogger<MonoWebhookController> _logger;

    public MonoWebhookController(IMediator mediator, IConfiguration config, ILogger<MonoWebhookController> logger)
    {
        _mediator = mediator;
        _config = config;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Receive([FromBody] JsonElement payload, CancellationToken ct)
    {
        var expected = _config["Mono:WebhookSecret"];
        if (string.IsNullOrWhiteSpace(expected)) return StatusCode(StatusCodes.Status503ServiceUnavailable);

        var sent = Request.Headers["mono-webhook-secret"].ToString();
        if (!FixedTimeEquals(sent, expected)) return Unauthorized();

        var accountId = FindAccountId(payload);
        if (accountId is not null)
        {
            try { await _mediator.Send(new SyncBankAccountByIdCommand(accountId), ct); }
            catch (Exception ex)
            {
                // Mono only needs to know we received it; the next sync picks anything missed up.
                _logger.LogWarning(ex, "Mono webhook sync failed for account {AccountId}", accountId);
            }
        }
        return Ok();
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    // The account id sits in a slightly different place per event, so look in the usual spots.
    private static string? FindAccountId(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
            return null;

        if (data.TryGetProperty("account", out var acct) && acct.ValueKind == JsonValueKind.Object)
            foreach (var name in new[] { "_id", "id" })
                if (acct.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();

        foreach (var name in new[] { "account_id", "accountId", "id" })
            if (data.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String) return v.GetString();

        return null;
    }
}
