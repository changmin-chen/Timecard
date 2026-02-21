using System.Security.Claims;

namespace Timecard.Api.Features.Auth;

public sealed class GoogleCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    // Google maps the stable 'sub' claim to ClaimTypes.NameIdentifier.
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
