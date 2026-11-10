using Ilevora.Application.Commands.Invitations;
using Ilevora.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ilevora.API.Controllers;

[ApiController]
[Authorize]
[Route("api/invitations")]
public class InvitationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public InvitationsController(IMediator mediator) => _mediator = mediator;

    /// <summary>The company's invitations with their status (admin or farm manager).</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct) => Ok(await _mediator.Send(new ListInvitationsQuery(), ct));

    [HttpPost]
    [EnableRateLimiting("invite")]
    public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request, CancellationToken ct) =>
        StatusCode(201, await _mediator.Send(new CreateInvitationCommand(request), ct));

    [HttpPost("{id}/resend")]
    [EnableRateLimiting("invite")]
    public async Task<IActionResult> Resend(string id, CancellationToken ct) => Ok(await _mediator.Send(new ResendInvitationCommand(id), ct));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Revoke(string id, CancellationToken ct) => Ok(await _mediator.Send(new RevokeInvitationCommand(id), ct));

    /// <summary>Who the invitation is for, so the accept page can show it. Anonymous: the token is the credential.</summary>
    [HttpPost("preview")]
    [AllowAnonymous]
    [EnableRateLimiting("token-endpoints")]
    public async Task<IActionResult> Preview([FromBody] PreviewInvitationRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetInvitationPreviewQuery(request.Token), ct));

    [HttpPost("accept")]
    [AllowAnonymous]
    [EnableRateLimiting("token-endpoints")]
    public async Task<IActionResult> Accept([FromBody] AcceptInvitationRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new AcceptInvitationCommand(request), ct));
}
