using Microsoft.AspNetCore.Mvc;
using Subly.Application.Services;

namespace Subly.Api.Controllers;

[ApiController]
[Route("api/admin")]
public sealed class AdminController(IAdminService adminService) : ControllerBase
{
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
