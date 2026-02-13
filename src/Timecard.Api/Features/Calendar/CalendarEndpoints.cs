using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Calendar;

public static class CalendarEndpoints
{
    private const string DefaultCalendarId = CalendarConstants.TaiwanDgpaCalendarId;

    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/calendar").WithTags("Calendar");

        g.MapGet("/{date}", GetByDate);
        g.MapPut("/{date}/override", UpsertOverride);
        g.MapDelete("/{date}/override", DeleteOverride);
        g.MapPost("/import/tw-dgpa", ImportTaiwanDgpa).DisableAntiforgery();

        return app;
    }

    public sealed record CalendarOverrideRequest(bool IsWorking, string? Kind, string? Note);

    private static async Task<IResult> GetByDate(IWorkCalendar calendar, string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var day = await calendar.GetDayAsync(DefaultCalendarId, d, ct);
        if (day is null) return Results.NotFound();

        return Results.Ok(new
        {
            date = d.ToString("yyyy-MM-dd"),
            calendarId = day.CalendarId,
            isWorking = day.IsWorking,
            kind = day.Kind,
            note = day.Note,
            source = day.Source,
            importedAt = day.VersionImportedAt
        });
    }

    private static async Task<IResult> UpsertOverride(TimecardDb db, string date, CalendarOverrideRequest req, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var now = DateTimeOffset.UtcNow;
        var existing = await db.CalendarDayOverrides.FindAsync([DefaultCalendarId, d], ct);
        if (existing is null)
        {
            db.CalendarDayOverrides.Add(new CalendarDayOverride
            {
                CalendarId = DefaultCalendarId,
                Date = d,
                IsWorking = req.IsWorking,
                Kind = req.Kind?.Trim() ?? "",
                Note = req.Note?.Trim() ?? "",
                Source = "ManualOverride",
                UpdatedAt = now
            });
        }
        else
        {
            existing.IsWorking = req.IsWorking;
            existing.Kind = req.Kind?.Trim() ?? "";
            existing.Note = req.Note?.Trim() ?? "";
            existing.Source = "ManualOverride";
            existing.UpdatedAt = now;
        }

        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteOverride(TimecardDb db, string date, CancellationToken ct)
    {
        if (!DateOnly.TryParse(date, out var d))
            return Results.BadRequest(new { error = "Invalid date. Use yyyy-MM-dd." });

        var existing = await db.CalendarDayOverrides.FindAsync([DefaultCalendarId, d], ct);
        if (existing is null) return Results.NotFound();

        db.CalendarDayOverrides.Remove(existing);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> ImportTaiwanDgpa(
        DgpaCalendarImporter importer,
        [FromForm] IFormFile? file,
        [FromForm] string? calendarId,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return Results.BadRequest(new { error = "CSV file is required. Use multipart/form-data with field name 'file'." });

        var targetCalendarId = string.IsNullOrWhiteSpace(calendarId) ? DefaultCalendarId : calendarId.Trim();

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await importer.ImportAsync(targetCalendarId, stream, ct);
            return Results.Ok(result);
        }
        catch (FormatException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (CsvHelperException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (DbUpdateException ex)
        {
            return Results.Problem(title: "Calendar import failed", detail: ex.Message, statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
