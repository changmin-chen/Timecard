using Microsoft.EntityFrameworkCore;

namespace Timecard.Api.Data;

public class WorkDayRepository(TimecardDb db)
{
    private readonly TimecardDb _db = db;

    public async Task<WorkDay?> LoadDay(DateOnly date, CancellationToken ct)
        => await _db.WorkDays
            .Include(d => d.Punches)
            .Include(d => d.Adjustments)
            .FirstOrDefaultAsync(d => d.Date == date, ct);

    public async Task<WorkDay> GetOrCreateDay(DateOnly date, CancellationToken ct)
    {
        var day = await _db.WorkDays.FirstOrDefaultAsync(d => d.Date == date, ct);
        if (day is not null) return day;

        day = new WorkDay { Date = date };
        _db.WorkDays.Add(day);
        await _db.SaveChangesAsync(ct);
        return day;
    }
}
