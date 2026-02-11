using Timecard.Api.Data;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.AttendanceRequests;

public static class AttendanceRequestEndpoints
{
    public static IEndpointRouteBuilder MapAttendanceRequestEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/attendance-requests").WithTags("AttendanceRequests");

        g.MapPost("", Create);
        g.MapPut("/{id:int}", Update);
        g.MapDelete("/{id:int}", Delete);

        return app;
    }

    public sealed record AttendanceRequestCreate(string Date, string Category, string Start, string End, string? Note);
    public sealed record AttendanceRequestUpdate(string Category, string Start, string End, string? Note);

    private static async Task<IResult> Create(WorkDayRepository repo, AttendanceRequestCreate req, CancellationToken ct)
    {
        if (!DateOnly.TryParse(req.Date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        if (!TimeOnly.TryParse(req.Start, out var start))
            return Results.BadRequest(new { error = "Invalid start time. Use HH:mm." });

        if (!TimeOnly.TryParse(req.End, out var end))
            return Results.BadRequest(new { error = "Invalid end time. Use HH:mm." });

        var day = await repo.GetOrCreateDay(d, ct);

        var result = day.AddAttendanceRequest(req.Category, start, end, req.Note);
        if (result.ToErrorResult() is { } err) return err;

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }

    private static async Task<IResult> Update(WorkDayRepository repo, int id, AttendanceRequestUpdate req, CancellationToken ct)
    {
        if (!TimeOnly.TryParse(req.Start, out var start))
            return Results.BadRequest(new { error = "Invalid start time. Use HH:mm." });

        if (!TimeOnly.TryParse(req.End, out var end))
            return Results.BadRequest(new { error = "Invalid end time. Use HH:mm." });

        var day = await repo.LoadByAttendanceRequestId(id, ct);
        if (day is null) return Results.NotFound();

        var result = day.UpdateAttendanceRequest(id, req.Category, start, end, req.Note);
        if (result.ToErrorResult() is { } err) return err;

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }

    private static async Task<IResult> Delete(WorkDayRepository repo, int id, CancellationToken ct)
    {
        var day = await repo.LoadByAttendanceRequestId(id, ct);
        if (day is null) return Results.NotFound();

        var result = day.RemoveAttendanceRequest(id);
        if (result.ToErrorResult() is { } err) return err;

        await repo.SaveChangesAsync(ct);
        return Results.Ok(Mapping.ToDayDto(day));
    }
}
