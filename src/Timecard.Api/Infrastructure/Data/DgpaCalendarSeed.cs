using Microsoft.EntityFrameworkCore;
using Timecard.Api.Features.Calendar;

namespace Timecard.Api.Infrastructure.Data;

public static class DgpaCalendarSeed
{
    private const string CsvRelativePath = "Seeds/115-dpga-gov-calendar.csv";

    public static async Task SeedAsync(TimecardDb db, ILogger logger, CancellationToken ct = default)
    {
        var csvPath = Path.IsPathRooted(CsvRelativePath)
            ? CsvRelativePath
            : Path.Combine(AppContext.BaseDirectory, CsvRelativePath);

        if (!File.Exists(csvPath))
        {
            logger.LogWarning("DGPA calendar seed file not found: {CsvPath}", csvPath);
            return;
        }

        var hasData = await db.CalendarDays
            .AsNoTracking()
            .AnyAsync(x => x.CalendarId == CalendarConstants.TaiwanDgpaCalendarId, ct);

        if (hasData)
        {
            logger.LogInformation(
                "DGPA calendar data already exists for {CalendarId}, skip seed.",
                CalendarConstants.TaiwanDgpaCalendarId);
            return;
        }

        await using var stream = File.OpenRead(csvPath);
        var importer = new DgpaCalendarImporter(db);
        var result = await importer.ImportAsync(CalendarConstants.TaiwanDgpaCalendarId, stream, ct);

        logger.LogInformation(
            "DGPA calendar seed imported. CalendarId={CalendarId}, Total={TotalRows}, Inserted={InsertedRows}, Updated={UpdatedRows}, ImportedAt={ImportedAtUtc:o}",
            result.CalendarId,
            result.TotalRows,
            result.InsertedRows,
            result.UpdatedRows,
            result.ImportedAtUtc);
    }
}
