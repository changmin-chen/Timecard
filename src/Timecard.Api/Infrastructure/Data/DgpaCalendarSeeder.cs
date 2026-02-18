using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Timecard.Api.Features.Calendar;

namespace Timecard.Api.Infrastructure.Data;

public sealed class DgpaCalendarSeedOptions
{
    public bool Enabled { get; set; } = true;

    public string CsvRelativePath { get; set; } = "Seeds/115-dpga-gov-calendar.csv";

    public bool ForceImport { get; set; }
}

public sealed class DgpaCalendarSeeder(
    TimecardDb db,
    DgpaCalendarImporter importer,
    IOptions<DgpaCalendarSeedOptions> options,
    ILogger<DgpaCalendarSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seedOptions = options.Value;

        if (!seedOptions.Enabled)
        {
            logger.LogInformation("DGPA calendar seed is disabled.");
            return;
        }

        var csvPath = ResolveCsvPath(seedOptions.CsvRelativePath);
        if (!File.Exists(csvPath))
        {
            logger.LogWarning("DGPA calendar seed file not found: {CsvPath}", csvPath);
            return;
        }

        if (!seedOptions.ForceImport)
        {
            var hasCalendarData = await db.CalendarDays
                .AsNoTracking()
                .AnyAsync(x => x.CalendarId == CalendarConstants.TaiwanDgpaCalendarId, ct);

            if (hasCalendarData)
            {
                logger.LogInformation(
                "DGPA calendar data already exists for {CalendarId}, skip seed.",
                CalendarConstants.TaiwanDgpaCalendarId);
                return;
            }
        }

        await using var stream = File.OpenRead(csvPath);
        var result = await importer.ImportAsync(CalendarConstants.TaiwanDgpaCalendarId, stream, ct);

        logger.LogInformation(
        "DGPA calendar seed imported. CalendarId={CalendarId}, Total={TotalRows}, Inserted={InsertedRows}, Updated={UpdatedRows}, ImportedAt={ImportedAtUtc:o}",
        result.CalendarId,
        result.TotalRows,
        result.InsertedRows,
        result.UpdatedRows,
        result.ImportedAtUtc);
    }

    private static string ResolveCsvPath(string configuredPath)
    {
        if (Path.IsPathRooted(configuredPath))
            return configuredPath;

        return Path.Combine(AppContext.BaseDirectory, configuredPath);
    }
}
