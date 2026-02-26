using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain;
using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.SyncPunch;

public sealed record SyncPunchEntry(string EmployeeId, DateTimeOffset At, string? Note);

public sealed record SyncPunchesRequest(List<SyncPunchEntry> Punches);

public sealed record SyncPunchesResult(int Processed, int Added, int Skipped, List<string> Errors);

public sealed class SyncPunchHandler(TimecardDb db, WorkDayRepository repo, ILogger<SyncPunchHandler> logger)
{
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(1);


    public async Task<IResult> Handle(SyncPunchesRequest req, HttpContext http, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var scope = logger.BeginScope(new Dictionary<string, object?>
        {
            ["TraceId"] = http.TraceIdentifier,
            ["PunchCount"] = req.Punches.Count
        });

        logger.LogInformation("SyncPunches started");

        var added = 0;
        var skipped = 0;
        var errors = new List<string>();
        var dayCache = new Dictionary<(string UserId, DateOnly Date), WorkDay>();

        var employeeIds = req.Punches.Select(p => p.EmployeeId).Distinct().ToList();

        var users = await db.Users
            .Where(u => employeeIds.Contains(u.EmployeeId))
            .ToDictionaryAsync(u => u.EmployeeId, ct);

        foreach (var entry in req.Punches)
        {
            if (!users.TryGetValue(entry.EmployeeId, out var user))
            {
                errors.Add($"[{entry.EmployeeId}] No user found with this employee ID.");
                logger.LogWarning("Unknown EmployeeId={EmployeeId}", entry.EmployeeId);
                continue;
            }

            var date = TaiwanTime.ToDate(entry.At);
            var key = (user.Id, date);

            if (!dayCache.TryGetValue(key, out var day))
            {
                day = await repo.GetOrCreateDay(user.Id, date, ct);
                dayCache[key] = day;
            }

            if (day.Punches.Any(p => Math.Abs((p.At - entry.At).TotalMinutes) < DedupWindow.TotalMinutes))
            {
                skipped++;
                continue;
            }

            var result = day.AddPunch(entry.At, entry.Note, TimeSpan.Zero, force: true);
            if (!result.IsSuccess)
            {
                errors.Add($"[{entry.EmployeeId}] {result.Error!.Message}");
                continue;
            }

            added++;
        }

        await repo.SaveChangesAsync(ct);

        logger.LogInformation(
        "SyncPunches completed Processed={Processed} Added={Added} Skipped={Skipped} ErrorCount={ErrorCount} ElapsedMs={ElapsedMs}",
        req.Punches.Count, added, skipped, errors.Count, sw.ElapsedMilliseconds);

        return Results.Ok(new SyncPunchesResult(req.Punches.Count, added, skipped, errors));
    }
}
