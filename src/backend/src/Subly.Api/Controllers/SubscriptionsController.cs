using Microsoft.AspNetCore.Mvc;
using Subly.Application.Contracts;
using Subly.Application.Services;
using Subly.Domain.Models;

namespace Subly.Api.Controllers;

[ApiController]
[Route("api/subscriptions")]
public sealed class SubscriptionsController(ISubscriptionService service) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<SubscriptionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<SubscriptionDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await service.GetSubscriptionsAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await service.GetSubscriptionAsync(id, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SubscriptionDto>> Create([FromBody] CreateSubscriptionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var created = await service.CreateSubscriptionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(SubscriptionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<SubscriptionDto>> UpdateStatus(
        Guid id,
        [FromBody] UpdateSubscriptionStatusRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var updated = await service.UpdateStatusAsync(id, request.Status, request.CancelledAt, cancellationToken);
            return updated is null ? NotFound() : Ok(updated);
        }
        catch (ArgumentException exception)
        {
            return ValidationProblem(detail: exception.Message);
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await service.DeleteSubscriptionAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
