using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CsvHelper;
using Timecard.Api.Features.Auth;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Calendar;

public static class CalendarEndpoints
{
    private const string DefaultCalendarId = CalendarConstants.TaiwanDgpaCalendarId;

    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/calendar").WithTags("Calendar");

        g.MapGet("/{date}", GetByDate);
        g.MapPost("/import/tw-dgpa", ImportTaiwanDgpa)
            .DisableAntiforgery()
            .RequireAuthorization(AuthRoles.Admin);

        return app;
    }

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
