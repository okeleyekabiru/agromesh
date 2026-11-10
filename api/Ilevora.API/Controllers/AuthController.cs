using Ilevora.Application.Commands.Auth;
using Ilevora.Application.DTOs;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Ilevora.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RegisterCommand(request), ct);
        return StatusCode(201, result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand(request), ct);
        return Ok(result);
    }

    /// <summary>Always 200 with the same body, whether or not the email has an account.</summary>
    [HttpPost("forgot-password")]
    [EnableRateLimiting("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await _mediator.Send(new ForgotPasswordCommand(request), ct);
        return Ok(new { message = "If an account exists for that email, a reset link is on its way." });
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("token-endpoints")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await _mediator.Send(new ResetPasswordCommand(request), ct);
        return Ok(new { message = "Your password has been changed. You can sign in now." });
    }
}
