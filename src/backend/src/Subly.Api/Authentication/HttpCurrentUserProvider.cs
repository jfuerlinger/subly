using System.Security.Claims;
using Subly.Application.Abstractions;

namespace Subly.Api.Authentication;

public sealed class HttpCurrentUserProvider(IHttpContextAccessor httpContextAccessor) : ICurrentUserProvider
{
    public Guid GetRequiredUserId()
    {
        var userIdClaim = httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? httpContextAccessor.HttpContext?.User.FindFirstValue("sub");

        if (!Guid.TryParse(userIdClaim, out var userId) || userId == Guid.Empty)
        {
            throw new InvalidOperationException("Authenticated user id is missing.");
        }

        return userId;
    }
}
