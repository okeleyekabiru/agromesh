using Ilevora.Application.Commands.Farm;
using Ilevora.Application.DTOs;
using Ilevora.Application.Queries.Farm;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ilevora.API.Controllers;

/// <summary>The contact and bank details a farm prints on its invoices. Read: any user of the company. Write: admin or farm manager.</summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/business-profile")]
public class FarmBusinessProfileController : ControllerBase
{
    private readonly IMediator _mediator;

    public FarmBusinessProfileController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(string farmId, CancellationToken ct) =>
        Ok(await _mediator.Send(new GetFarmBusinessProfileQuery(farmId), ct));

    [HttpPut]
    public async Task<IActionResult> Update(string farmId, [FromBody] UpdateFarmBusinessProfileRequest request, CancellationToken ct) =>
        Ok(await _mediator.Send(new UpdateFarmBusinessProfileCommand(farmId, request), ct));
}
