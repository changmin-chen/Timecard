using Ardalis.GuardClauses;
using Microsoft.AspNetCore.Identity;

namespace Timecard.Api.Domain.Entities;

public sealed class AppUser : IdentityUser
{
    public AppUser()
    {
    }

    public AppUser(string email, string employeeId, string? displayName = null)
    {
        Guard.Against.NullOrWhiteSpace(email);
        Guard.Against.NullOrWhiteSpace(employeeId);

        Email = email.Trim();
        UserName = Email;
        EmployeeId = employeeId.Trim();
        DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim();
        EmailConfirmed = true;
    }

    public string EmployeeId { get; set; } = ""; // Maps to punch clock employee ID
    public string? DisplayName { get; set; }
    public bool MustChangePassword { get; set; } // true for admin-created accounts
}
