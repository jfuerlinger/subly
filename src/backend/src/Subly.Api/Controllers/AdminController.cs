using Microsoft.AspNetCore.Mvc;
using Subly.Application.Contracts;
using Subly.Application.Services;

namespace Subly.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(IAdminService adminService, IHostEnvironment hostEnvironment) : ControllerBase
{
    [HttpPost("reset-database")]
    [ProducesResponseType(typeof(DatabaseResetResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<DatabaseResetResultDto>> ResetDatabase(CancellationToken cancellationToken)
    {
        if (!hostEnvironment.IsDevelopment() && !hostEnvironment.IsEnvironment("Testing"))
        {
            return Problem(
                title: "Database reset is not allowed in this environment.",
                statusCode: StatusCodes.Status403Forbidden);
        }

        var result = await adminService.ResetDatabaseAsync(cancellationToken);
        return Ok(result);
    }

    [HttpDelete("data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAllData(CancellationToken cancellationToken)
    {
        await adminService.DeleteAllDataAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SeedData(CancellationToken cancellationToken)
    {
        await adminService.SeedDataAsync(cancellationToken);
        return NoContent();
    }
}
