using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Entities.WorkDayAggregate;

namespace Timecard.Api.Infrastructure.Data;

public sealed class WorkDayRepository(TimecardDb db)
{
    public Task<WorkDay?> LoadDay(string userId, DateOnly date, CancellationToken ct)
        => db.WorkDays
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .AsSplitQuery()
            .FirstOrDefaultAsync(d => d.UserId == userId && d.Date == date, ct);

    public Task<WorkDay?> LoadByPunchId(string userId, int punchId, CancellationToken ct)
        => db.WorkDays
            .Where(d => d.UserId == userId && d.Punches.Any(p => p.Id == punchId))
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

    public Task<WorkDay?> LoadByAttendanceRequestId(string userId, int requestId, CancellationToken ct)
        => db.WorkDays
            .Where(d => d.UserId == userId && d.AttendanceRequests.Any(a => a.Id == requestId))
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .AsSplitQuery()
            .FirstOrDefaultAsync(ct);

    public async Task<WorkDay> GetOrCreateDay(string userId, DateOnly date, CancellationToken ct)
    {
        var localDay = db.WorkDays.Local.FirstOrDefault(d => d.UserId == userId && d.Date == date);
        if (localDay is not null)
        {
            await EnsureDayAggregateLoadedAsync(localDay, ct);
            return localDay;
        }

        var day = await LoadDay(userId, date, ct);
        if (day is not null) return day;

        await db.Database.ExecuteSqlInterpolatedAsync($"""
                                                       INSERT INTO "WorkDays" ("UserId", "Date")
                                                       VALUES ({userId}, {date})
                                                       ON CONFLICT ("UserId", "Date") DO NOTHING;
                                                       """, ct);

        day = await LoadDay(userId, date, ct);
        if (day is null)
        {
            throw new InvalidOperationException(
            $"Failed to load WorkDay for user '{userId}' on '{date:yyyy-MM-dd}' after upsert.");
        }

        return day;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);

    private async Task EnsureDayAggregateLoadedAsync(WorkDay day, CancellationToken ct)
    {
        var entry = db.Entry(day);
        if (entry.State == EntityState.Added)
            return;

        if (!entry.Collection(d => d.Punches).IsLoaded)
            await entry.Collection(d => d.Punches).LoadAsync(ct);

        if (!entry.Collection(d => d.AttendanceRequests).IsLoaded)
            await entry.Collection(d => d.AttendanceRequests).LoadAsync(ct);
    }
}
