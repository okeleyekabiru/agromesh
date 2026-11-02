using Ilevora.Application.Queries.Inventory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Ilevora.API.Controllers;

[ApiController]
[Route("api/chemicals")]
public class ChemicalsController : ControllerBase
{
    private readonly IMediator _mediator;

    public ChemicalsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct) =>
        Ok(await _mediator.Send(new GetChemicalsQuery(), ct));
}
