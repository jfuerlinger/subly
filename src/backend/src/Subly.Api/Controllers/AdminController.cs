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
        if (EnsureNonProductionEnvironment() is { } forbidden)
        {
            return forbidden;
        }

        var result = await adminService.ResetDatabaseAsync(cancellationToken);
        return Ok(result);
    }

    [HttpDelete("data")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> DeleteAllData(CancellationToken cancellationToken)
    {
        if (EnsureNonProductionEnvironment() is { } forbidden)
        {
            return forbidden;
        }

        await adminService.DeleteAllDataAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("seed")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> SeedData(CancellationToken cancellationToken)
    {
        if (EnsureNonProductionEnvironment() is { } forbidden)
        {
            return forbidden;
        }

        await adminService.SeedDataAsync(cancellationToken);
        return NoContent();
    }

    // These admin endpoints have no user/role concept to authorize against (see ICurrentUserProvider) —
    // restricting them to Development/Testing is the only guard preventing anyone from wiping production data.
    private ObjectResult? EnsureNonProductionEnvironment()
    {
        if (hostEnvironment.IsDevelopment() || hostEnvironment.IsEnvironment("Testing"))
        {
            return null;
        }

        return Problem(
            title: "This operation is not allowed in this environment.",
            statusCode: StatusCodes.Status403Forbidden);
    }
}
