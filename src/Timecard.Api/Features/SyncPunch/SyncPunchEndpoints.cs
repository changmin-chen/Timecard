using Microsoft.EntityFrameworkCore;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.SyncPunch;

public static class SyncPunchEndpoints
{
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(1);

    public static IEndpointRouteBuilder MapSyncPunchEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/sync-punch").WithTags("SyncPunch").AllowAnonymous();
        g.MapPost("/punches", SyncPunches).AddEndpointFilter<ApiKeyEndpointFilter>();
        return app;
    }

    public sealed record SyncPunchEntry(string EmployeeId, DateTimeOffset At, string? Note);

    public sealed record SyncPunchesRequest(List<SyncPunchEntry> Punches);

    public sealed record SyncPunchesResult(int Processed, int Added, int Skipped, List<string> Errors);

    private static async Task<IResult> SyncPunches(
        SyncPunchesRequest req,
        HttpContext http,
        IConfiguration config,
        TimecardDb db,
        WorkDayRepository repo,
        CancellationToken ct)
    {
        var added = 0;
        var skipped = 0;
        var errors = new List<string>();

        // 批次查詢，避免 N+1
        var employeeIds = req.Punches.Select(p => p.EmployeeId).Distinct().ToList();
        var users = await db.Users
            .Where(u => u.EmployeeId != null && employeeIds.Contains(u.EmployeeId))
            .ToDictionaryAsync(u => u.EmployeeId!, ct);

        foreach (var entry in req.Punches)
        {
            if (!users.TryGetValue(entry.EmployeeId, out var user))
            {
                errors.Add($"[{entry.EmployeeId}] No user found with this employee ID.");
                continue;
            }

            var date = DateOnly.FromDateTime(entry.At.LocalDateTime);
            var day = await repo.GetOrCreateDay(user.Id, date, ct);

            // Idempotency: skip if a punch within DedupWindow already exists
            if (day.Punches.Any(p => Math.Abs((p.At - entry.At).TotalMinutes) < DedupWindow.TotalMinutes))
            {
                skipped++;
                continue;
            }

            var result = day.AddPunch(entry.At.ToUniversalTime(), entry.Note, TimeSpan.Zero, force: true);
            if (!result.IsSuccess)
            {
                errors.Add($"[{entry.EmployeeId}] {result.Error!.Message}");
                continue;
            }

            added++;
        }

        await repo.SaveChangesAsync(ct);
        return Results.Ok(new SyncPunchesResult(req.Punches.Count, added, skipped, errors));
    }
}
