using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Shared;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.SyncPunch;

public static class SyncPunchEndpoints
{
    private static readonly TimeSpan DedupWindow = TimeSpan.FromMinutes(1);

    public static IEndpointRouteBuilder MapSyncPunchEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/sync-punch").WithTags("SyncPunch").AllowAnonymous();
        g.MapPost("/punches", SyncPunches)
            .AddEndpointFilter<ApiKeyEndpointFilter>()
            .AddEndpointFilter<SyncPunchesValidationFilter>();
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
        var dayCache = new Dictionary<(string UserId, DateOnly Date), WorkDay>();

        // 批次查詢，避免 N+1
        var employeeIds = req.Punches.Select(p => p.EmployeeId).Distinct().ToList();
        var users = await db.Users
            .Where(u => employeeIds.Contains(u.EmployeeId))
            .ToDictionaryAsync(u => u.EmployeeId, ct);

        foreach (var entry in req.Punches)
        {
            if (!users.TryGetValue(entry.EmployeeId, out var user))
            {
                errors.Add($"[{entry.EmployeeId}] No user found with this employee ID.");
                continue;
            }

            var date = DateOnly.FromDateTime(entry.At.LocalDateTime);
            var key = (user.Id, date);
            if (!dayCache.TryGetValue(key, out var day))
            {
                day = await repo.GetOrCreateDay(user.Id, date, ct);
                dayCache[key] = day;
            }

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

internal sealed class SyncPunchesValidationFilter : IEndpointFilter
{
    private const int MaxPunchesPerRequest = 5000;
    
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var req = ctx.GetArgument<SyncPunchEndpoints.SyncPunchesRequest>(0);

        if (req.Punches.Count == 0)
            return new Error("sync.empty_punches", "At least one punch entry is required.",
                ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);

        if (req.Punches.Count > MaxPunchesPerRequest)
            return new Error("sync.too_many_punches", $"Maximum {MaxPunchesPerRequest} punches are allowed per request.",
            ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);
            
        var badIndexes = req.Punches
            .Select((e, i) => (e, i))
            .Where(x => string.IsNullOrWhiteSpace(x.e.EmployeeId))
            .Select(x => x.i)
            .ToList();

        if (badIndexes.Count > 0)
            return new Error("sync.invalid_employee_id",
                $"EmployeeId is required for entries: {string.Join(", ", badIndexes)}.",
                ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);

        return await next(ctx);
    }
}
