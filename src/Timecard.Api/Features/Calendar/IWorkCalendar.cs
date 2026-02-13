namespace Timecard.Api.Features.Calendar;

public interface IWorkCalendar
{
    Task<ResolvedCalendarDay?> GetDayAsync(string calendarId, DateOnly date, CancellationToken ct);
    Task<bool> IsWorkingDayAsync(string calendarId, DateOnly date, CancellationToken ct);
    Task<IReadOnlyDictionary<DateOnly, ResolvedCalendarDay>> GetDaysAsync(string calendarId, DateOnly startInclusive, DateOnly endExclusive, CancellationToken ct);
    Task<ResolvedCalendarDay> GetRequiredDayAsync(string calendarId, DateOnly date, CancellationToken ct);
    Task<IReadOnlyDictionary<DateOnly, ResolvedCalendarDay>> GetRequiredDaysAsync(string calendarId, DateOnly startInclusive, DateOnly endExclusive, CancellationToken ct);
}

public sealed record ResolvedCalendarDay(
    string CalendarId,
    DateOnly Date,
    bool IsWorking,
    string Kind,
    string Note,
    string Source,
    DateTimeOffset VersionImportedAt
);

