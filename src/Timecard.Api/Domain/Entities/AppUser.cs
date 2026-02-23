using Ardalis.GuardClauses;

namespace Timecard.Api.Domain.Entities;

public sealed class AppUser : BaseEntity<string>
{
    private AppUser()
    {
    }

    public AppUser(string id, string email, string employeeId, string? displayName = null)
    {
        Guard.Against.NullOrWhiteSpace(id);
        Guard.Against.NullOrWhiteSpace(email);
        Guard.Against.NullOrWhiteSpace(employeeId);

        Id = id.Trim();
        Email = email.Trim();
        EmployeeId = employeeId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
    }

    public string Email { get; private set; } = "";
    public string EmployeeId { get; set; } = "";            // Maps to punch clock employee ID
    public string? DisplayName { get; private set; }
    public string? PasswordHash { get; set; }               // PBKDF2 hash via IPasswordHasher<AppUser>
    public bool MustChangePassword { get; set; }            // true for admin-created accounts
    public bool IsAdmin { get; set; }                       // grants access to admin endpoints
}
