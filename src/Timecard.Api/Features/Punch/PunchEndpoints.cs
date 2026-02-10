using Timecard.Api.Data;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.Punch;

public static class PunchEndpoints
{
    private static readonly TimeSpan MinInterval = TimeSpan.FromSeconds(30);

    public static IEndpointRouteBuilder MapPunchEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/punch").WithTags("Punch");

        g.MapPost("", AddPunch);
        g.MapGet("/status", GetStatus);

        var p = app.MapGroup("/api/punches").WithTags("Punches");
        p.MapPut("/{id:int}", UpdatePunch);
        p.MapDelete("/{id:int}", DeletePunch);

        return app;
    }

    public sealed record PunchCreate(DateTimeOffset? At, string? Note, bool Force);
    public sealed record PunchUpdate(DateTimeOffset At, string? Note);

    private static async Task<IResult> AddPunch(WorkDayRepository repo, PunchCreate? req, CancellationToken ct)
    {
        var now = req?.At ?? DateTimeOffset.UtcNow;
        var date = DateOnly.FromDateTime(now.LocalDateTime);
        var day = await repo.GetOrCreateDay(date, ct);

        var result = day.AddPunch(now, req?.Note, MinInterval, req?.Force == true);
        if (result.ToErrorResult() is { } err) return err;

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }

    private static async Task<IResult> UpdatePunch(WorkDayRepository repo, int id, PunchUpdate req, CancellationToken ct)
    {
        var day = await repo.LoadByPunchId(id, ct);
        if (day is null) return Results.NotFound();

        var result = day.UpdatePunch(id, req.At, req.Note);
        if (result.ToErrorResult() is { } err) return err;

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }

    private static async Task<IResult> DeletePunch(WorkDayRepository repo, int id, CancellationToken ct)
    {
        var day = await repo.LoadByPunchId(id, ct);
        if (day is null) return Results.NotFound();

        var result = day.RemovePunch(id);
        if (result.ToErrorResult() is { } err) return err;

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }

    private static async Task<IResult> GetStatus(WorkDayRepository repo, string? date, CancellationToken ct)
    {
        var d = ParseDateOrToday(date);
        var day = await repo.LoadDay(d, ct);

        var punches = day?.Punches.OrderBy(p => p.At).ToList() ?? [];
        var (start, end, worked) = day?.DeriveSpan() ?? (null, null, 0);

        return Results.Ok(new
        {
            Date = d.ToString("yyyy-MM-dd"),
            PunchCount = punches.Count,
            Start = start,
            End = end,
            WorkedMinutes = worked
        });
    }

    private static DateOnly ParseDateOrToday(string? s)
    {
        if (!string.IsNullOrWhiteSpace(s) && DateOnly.TryParse(s, out var d)) return d;
        return DateOnly.FromDateTime(DateTime.Now);
    }
}
