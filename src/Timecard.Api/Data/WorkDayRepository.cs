using Microsoft.EntityFrameworkCore;

namespace Timecard.Api.Data;

public sealed class WorkDayRepository(TimecardDb db)
{
    public Task<WorkDay?> LoadDay(DateOnly date, CancellationToken ct)
        => db.WorkDays
            .Include(d => d.Punches)
            .Include(d => d.Adjustments)
            .FirstOrDefaultAsync(d => d.Date == date, ct);

    public Task<WorkDay?> LoadByPunchId(int punchId, CancellationToken ct)
        => db.WorkDays
            .Where(d => d.Punches.Any(p => p.Id == punchId))
            .Include(d => d.Punches)
            .Include(d => d.Adjustments)
            .FirstOrDefaultAsync(ct);

    public Task<WorkDay?> LoadByAdjustmentId(int adjustmentId, CancellationToken ct)
        => db.WorkDays
            .Where(d => d.Adjustments.Any(a => a.Id == adjustmentId))
            .Include(d => d.Punches)
            .Include(d => d.Adjustments)
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
