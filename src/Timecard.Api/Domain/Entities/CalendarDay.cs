namespace Timecard.Api.Domain.Entities;

public sealed class CalendarDay
{
    public required string CalendarId { get; init; }
    public required DateOnly Date { get; init; }
    public required bool IsWorking { get; set; }
    public string Kind { get; set; } = "";
    public string Note { get; set; } = "";
    public string Source { get; set; } = "";
    public DateTimeOffset VersionImportedAt { get; set; }
}
