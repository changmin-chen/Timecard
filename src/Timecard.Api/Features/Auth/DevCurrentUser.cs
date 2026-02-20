namespace Timecard.Api.Features.Auth;

// Phase 1 placeholder — replaced by real auth in Phase 2.
// UserId must match a record in the AppUsers table (seeded on startup).
public sealed class DevCurrentUser : ICurrentUser
{
    public const string Id = "dev-placeholder";
    public string UserId => Id;
}
