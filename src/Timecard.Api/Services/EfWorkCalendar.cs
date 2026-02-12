using Microsoft.EntityFrameworkCore;
using Timecard.Api.Data;
using Timecard.Api.Data.Entities;

namespace Timecard.Api.Services;

public sealed class EfWorkCalendar(TimecardDb db) : IWorkCalendar
{
    public async Task<ResolvedCalendarDay?> GetDayAsync(string calendarId, DateOnly date, CancellationToken ct)
    {
        var baseDay = await db.CalendarDays
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date == date, ct);

        var overrideDay = await db.CalendarDayOverrides
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date == date, ct);

        return Resolve(baseDay, overrideDay);
    }

    public async Task<bool> IsWorkingDayAsync(string calendarId, DateOnly date, CancellationToken ct)
    {
        var day = await GetDayAsync(calendarId, date, ct);
        if (day is null)
            throw new InvalidOperationException($"Calendar data missing: {calendarId} {date:yyyy-MM-dd}");

        return day.IsWorking;
    }

    public async Task<IReadOnlyDictionary<DateOnly, ResolvedCalendarDay>> GetDaysAsync(
        string calendarId,
        DateOnly startInclusive,
        DateOnly endExclusive,
        CancellationToken ct)
    {
        var baseDays = await db.CalendarDays
            .AsNoTracking()
            .Where(d => d.CalendarId == calendarId && d.Date >= startInclusive && d.Date < endExclusive)
            .ToDictionaryAsync(d => d.Date, ct);

        var overrideDays = await db.CalendarDayOverrides
            .AsNoTracking()
            .Where(d => d.CalendarId == calendarId && d.Date >= startInclusive && d.Date < endExclusive)
            .ToDictionaryAsync(d => d.Date, ct);

        var allDates = baseDays.Keys.Union(overrideDays.Keys);
        var resolved = new Dictionary<DateOnly, ResolvedCalendarDay>();

        foreach (var date in allDates)
        {
            baseDays.TryGetValue(date, out var baseDay);
            overrideDays.TryGetValue(date, out var overrideDay);

            var day = Resolve(baseDay, overrideDay);
            if (day is not null)
                resolved[date] = day;
        }

        return resolved;
    }

    public async Task<ResolvedCalendarDay> GetRequiredDayAsync(string calendarId, DateOnly date, CancellationToken ct)
    {
        var day = await GetDayAsync(calendarId, date, ct);
        if (day is null)
            throw new InvalidOperationException($"Calendar data missing: {calendarId} {date:yyyy-MM-dd}");

        return day;
    }

    public async Task<IReadOnlyDictionary<DateOnly, ResolvedCalendarDay>> GetRequiredDaysAsync(
        string calendarId,
        DateOnly startInclusive,
        DateOnly endExclusive,
        CancellationToken ct)
    {
        var days = await GetDaysAsync(calendarId, startInclusive, endExclusive, ct);
        for (var d = startInclusive; d < endExclusive; d = d.AddDays(1))
        {
            if (!days.ContainsKey(d))
                throw new InvalidOperationException($"Calendar data missing: {calendarId} {d:yyyy-MM-dd}");
        }

        return days;
    }

    private static ResolvedCalendarDay? Resolve(CalendarDay? baseDay, CalendarDayOverride? overrideDay)
    {
        if (baseDay is null && overrideDay is null)
            return null;

        if (overrideDay is not null)
        {
            return new ResolvedCalendarDay(
                CalendarId: overrideDay.CalendarId,
                Date: overrideDay.Date,
                IsWorking: overrideDay.IsWorking,
                Kind: string.IsNullOrWhiteSpace(overrideDay.Kind) ? baseDay?.Kind ?? "Override" : overrideDay.Kind,
                Note: string.IsNullOrWhiteSpace(overrideDay.Note) ? baseDay?.Note ?? "" : overrideDay.Note,
                Source: string.IsNullOrWhiteSpace(overrideDay.Source) ? "ManualOverride" : overrideDay.Source,
                VersionImportedAt: overrideDay.UpdatedAt
            );
        }

        return new ResolvedCalendarDay(
            CalendarId: baseDay!.CalendarId,
            Date: baseDay.Date,
            IsWorking: baseDay.IsWorking,
            Kind: baseDay.Kind,
            Note: baseDay.Note,
            Source: baseDay.Source,
            VersionImportedAt: baseDay.VersionImportedAt
        );
    }
}
