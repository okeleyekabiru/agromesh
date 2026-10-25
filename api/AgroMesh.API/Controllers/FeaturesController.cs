using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroMesh.API.Controllers;

/// <summary>Which parts of the app a farm has switched on. Every module is on for every farm for now.</summary>
[ApiController]
[Authorize]
[Route("api/farms/{farmId}/features")]
public class FeaturesController : ControllerBase
{
    private static readonly string[] All =
        ["finance", "inventory", "workers", "reports", "scheduling", "weather", "ai_categorization"];

    [HttpGet]
    public IActionResult Get(string farmId) => Ok(All.Select(n => new { name = n, enabled = true }));
}
