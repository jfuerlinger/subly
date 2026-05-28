using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subly.Application.Contracts;
using Subly.Application.Services;

namespace Subly.Api.Controllers;

[ApiController]
[Route("api/dashboard")]
[Authorize]
public sealed class DashboardController(ISubscriptionService service) : ControllerBase
{
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await service.GetDashboardSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
