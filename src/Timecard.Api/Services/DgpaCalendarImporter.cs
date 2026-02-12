using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration.Attributes;
using Timecard.Api.Data;
using Timecard.Api.Data.Entities;

namespace Timecard.Api.Services;

/// <summary>
/// Provides functionality to import calendar data from a CSV file into the calendar database.
/// The data is expected to be in the specific format provided by the Directorate-General of Personnel Administration (DGPA) in Taiwan.
/// </summary>
public sealed class DgpaCalendarImporter(TimecardDb db)
{
    private static readonly CultureInfo TaiwanCulture = CultureInfo.GetCultureInfo("zh-TW");

    public async Task<CalendarImportResult> ImportAsync(string calendarId, Stream csvStream, CancellationToken ct)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        using var csv = new CsvReader(reader, TaiwanCulture);

        var rows = csv.GetRecords<DgpaRow>().ToList();
        var now = DateTimeOffset.UtcNow;
        var inserted = 0;
        var updated = 0;

        foreach (var row in rows)
        {
            if (!TryParseDate(row.DateText, out var date))
                throw new FormatException($"Bad date: '{row.DateText}'.");

            if (!int.TryParse(row.HolidayFlagText, out var holidayFlag))
                throw new FormatException($"Bad holiday flag: '{row.HolidayFlagText}'.");

            var isWorking = holidayFlag == 0;
            var note = (row.Note ?? "").Trim();
            var kind = ClassifyKind(isWorking, note);

            var existing = await db.CalendarDays.FindAsync([calendarId, date], ct);
            if (existing is null)
            {
                db.CalendarDays.Add(new CalendarDay
                {
                    CalendarId = calendarId,
                    Date = date,
                    IsWorking = isWorking,
                    Kind = kind,
                    Note = note,
                    Source = "DGPA",
                    VersionImportedAt = now
                });
                inserted++;
                continue;
            }

            existing.IsWorking = isWorking;
            existing.Kind = kind;
            existing.Note = note;
            existing.Source = "DGPA";
            existing.VersionImportedAt = now;
            updated++;
        }

        await db.SaveChangesAsync(ct);
        return new CalendarImportResult(calendarId, rows.Count, inserted, updated, now);
    }

    private static readonly string[] DateFormats =
    [
        "yyyyMMdd",
        "yyyy/MM/dd",
        "yyyy-MM-dd",
        "yyyy.MM.dd",
    ];

    private static bool TryParseDate(string raw, out DateOnly date)
    {
        raw = raw.Trim();

        // 明確格式 (西曆)
        if (DateOnly.TryParseExact(raw, DateFormats,
            CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            return true;

        // Fallback: TaiwanCulture 處理民國年
        if (DateOnly.TryParse(raw, TaiwanCulture, DateTimeStyles.None, out date))
            return true;

        date = default;
        return false;
    }

    private static string ClassifyKind(bool isWorking, string note)
    {
        if (isWorking && note.Contains("補", StringComparison.Ordinal))
            return "MakeupWorkday";

        if (!isWorking && (note.Contains("星期", StringComparison.Ordinal) || note.Contains("週", StringComparison.Ordinal)))
            return "Weekend";

        return isWorking ? "WorkingDay" : "PublicHoliday";
    }
}

public sealed class DgpaRow
{
    [Name("西元日期", "日期", "Date")]
    public string DateText { get; set; } = "";

    [Name("是否放假", "IsHoliday")]
    public string HolidayFlagText { get; set; } = "";

    [Name("備註", "說明", "Note")]
    public string? Note { get; set; }
}

public sealed record CalendarImportResult(
    string CalendarId,
    int TotalRows,
    int InsertedRows,
    int UpdatedRows,
    DateTimeOffset ImportedAtUtc
);
