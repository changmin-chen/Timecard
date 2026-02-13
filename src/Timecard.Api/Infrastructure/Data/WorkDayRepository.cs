using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;

namespace Timecard.Api.Infrastructure.Data;

public sealed class WorkDayRepository(TimecardDb db)
{
    public Task<WorkDay?> LoadDay(DateOnly date, CancellationToken ct)
        => db.WorkDays
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .FirstOrDefaultAsync(d => d.Date == date, ct);

    public Task<WorkDay?> LoadByPunchId(int punchId, CancellationToken ct)
        => db.WorkDays
            .Where(d => d.Punches.Any(p => p.Id == punchId))
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .FirstOrDefaultAsync(ct);

    public Task<WorkDay?> LoadByAttendanceRequestId(int requestId, CancellationToken ct)
        => db.WorkDays
            .Where(d => d.AttendanceRequests.Any(a => a.Id == requestId))
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .FirstOrDefaultAsync(ct);

    public async Task<WorkDay> GetOrCreateDay(DateOnly date, CancellationToken ct)
    {
        var day = await LoadDay(date, ct);
        if (day is not null) return day;

        day = new WorkDay(date);
        db.WorkDays.Add(day);
        return day;
    }

    public Task SaveChangesAsync(CancellationToken ct)
        => db.SaveChangesAsync(ct);
}
