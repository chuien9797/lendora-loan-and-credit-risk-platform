using Lendora.Api.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Lendora.Api.Controllers;

[ApiController]
[Route("api/platform")]
public sealed class PlatformController : ControllerBase
{
    [HttpGet("info")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public IActionResult GetInfo()
    {
        var payload = new
        {
            Name = "Lendora API",
            Stage = "Stage 3 - Loan Application Workflow",
            Environment = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().EnvironmentName,
            UtcTime = DateTime.UtcNow
        };

        return Ok(ApiResponse<object>.Ok(payload, "Platform information retrieved.", HttpContext.TraceIdentifier));
    }
}
