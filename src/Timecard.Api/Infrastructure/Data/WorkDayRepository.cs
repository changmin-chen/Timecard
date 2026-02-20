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
        var day = await LoadDay(userId, date, ct);
        if (day is not null) return day;

        day = new WorkDay(userId, date);
        db.WorkDays.Add(day);
        return day;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);
}
