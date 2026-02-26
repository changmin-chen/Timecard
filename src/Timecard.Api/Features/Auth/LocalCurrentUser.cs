using System.Security.Claims;

namespace Timecard.Api.Features.Auth;

public sealed class LocalCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string UserId =>
        httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? throw new UnauthorizedAccessException("User is not authenticated.");
}
