using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Results;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Calendar;

public sealed class EfWorkCalendar(TimecardDb db) : IWorkCalendar
{
    public async Task<ResolvedCalendarDay?> GetDayAsync(string calendarId, DateOnly date, CancellationToken ct)
    {
        var day = await db.CalendarDays
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.CalendarId == calendarId && d.Date == date, ct);

        return day is null ? null : ToResolved(day);
    }

    public async Task<Result<bool>> IsWorkingDayAsync(string calendarId, DateOnly date, CancellationToken ct)
    {
        var day = await GetDayAsync(calendarId, date, ct);
        if (day is null) return Errors.Calendar.DataMissing;
        return day.IsWorking;
    }

    public async Task<IReadOnlyDictionary<DateOnly, ResolvedCalendarDay>> GetDaysAsync(
        string calendarId,
        DateOnly startInclusive,
        DateOnly endExclusive,
        CancellationToken ct)
    {
        return await db.CalendarDays
            .AsNoTracking()
            .Where(d => d.CalendarId == calendarId && d.Date >= startInclusive && d.Date < endExclusive)
            .ToDictionaryAsync(d => d.Date, d => ToResolved(d), ct);
    }

    public async Task<Result<ResolvedCalendarDay>> GetRequiredDayAsync(string calendarId, DateOnly date, CancellationToken ct)
    {
        var day = await GetDayAsync(calendarId, date, ct);
        if (day is null) return Errors.Calendar.DataMissing;
        return day;
    }

    public async Task<Result<IReadOnlyDictionary<DateOnly, ResolvedCalendarDay>>> GetRequiredDaysAsync(
        string calendarId,
        DateOnly startInclusive,
        DateOnly endExclusive,
        CancellationToken ct)
    {
        var days = await GetDaysAsync(calendarId, startInclusive, endExclusive, ct);
        for (var d = startInclusive; d < endExclusive; d = d.AddDays(1))
        {
            if (!days.ContainsKey(d))
                return Errors.Calendar.DataMissing;
        }

        return Result<IReadOnlyDictionary<DateOnly, ResolvedCalendarDay>>.Ok(days);
    }

    private static ResolvedCalendarDay ToResolved(CalendarDay day) => new(
        CalendarId: day.CalendarId,
        Date: day.Date,
        IsWorking: day.IsWorking,
        Kind: day.Kind,
        Note: day.Note,
        Source: day.Source,
        VersionImportedAt: day.VersionImportedAt
    );
}
