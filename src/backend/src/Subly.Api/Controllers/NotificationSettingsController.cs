using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subly.Application.Contracts;
using Subly.Application.Services;

namespace Subly.Api.Controllers;

[ApiController]
[Route("api/notification-settings")]
[Authorize]
public sealed class NotificationSettingsController(INotificationSettingsService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<NotificationSettingsDto>> Get(CancellationToken cancellationToken)
    {
        var result = await service.GetForCurrentUserAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    [ProducesResponseType(typeof(NotificationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NotificationSettingsDto>> Update([FromBody] UpdateNotificationSettingsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var result = await service.UpdateAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }
}
