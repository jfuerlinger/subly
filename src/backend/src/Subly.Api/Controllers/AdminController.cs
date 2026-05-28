using Microsoft.AspNetCore.Mvc;
using Subly.Application.Contracts;
using Subly.Application.Services;

namespace Subly.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(IAdminService adminService) : ControllerBase
{
    [HttpPost("reset-database")]
    [ProducesResponseType(typeof(DatabaseResetResultDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DatabaseResetResultDto>> ResetDatabase(CancellationToken cancellationToken)
    {
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
