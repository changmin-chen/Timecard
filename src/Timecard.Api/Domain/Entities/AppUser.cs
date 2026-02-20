namespace Timecard.Api.Domain.Entities;

public sealed class AppUser
{
    public string Id { get; set; } = "";           // Google sub claim (stable unique ID)
    public string Email { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string? EmployeeId { get; set; }        // Maps to punch clock employee ID
}
