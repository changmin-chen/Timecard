namespace Timecard.Api.Domain.Entities;

public sealed class AppUser
{
    public string Id { get; set; } = "";           // Stable unique ID (GUID string)
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? EmployeeId { get; set; }        // Maps to punch clock employee ID
    public string? PasswordHash { get; set; }      // PBKDF2 hash via IPasswordHasher<AppUser>
    public bool MustChangePassword { get; set; }   // true for admin-created accounts
    public bool IsAdmin { get; set; }              // grants access to admin endpoints
}
