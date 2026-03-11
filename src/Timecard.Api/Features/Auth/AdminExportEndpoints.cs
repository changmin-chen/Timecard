using System.Globalization;
using System.Text;
using CsvHelper;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain;
using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Features.Calendar;
using Timecard.Api.Features.Month;
using Timecard.Api.Features.Shared;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Auth;

public static class AdminExportEndpoints
{
    private const string CalendarId = CalendarConstants.TaiwanDgpaCalendarId;
    private static readonly string[] ChineseWeekdays = ["日", "一", "二", "三", "四", "五", "六"];

    public static IEndpointRouteBuilder MapAdminExportEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization(AuthRoles.Admin);
        g.MapGet("/export/month/{year:int}/{month:int}", ExportMonth);
        return app;
    }

    private static async Task<IResult> ExportMonth(
        TimecardDb db, IWorkCalendar calendar,
        HttpContext http, int year, int month, CancellationToken ct)
    {
        if (year is < 2000 or > 2100) return Results.BadRequest(new { error = "year out of range." });
        if (month is < 1 or > 12) return Results.BadRequest(new { error = "month out of range." });

        var start = new DateOnly(year, month, 1);
        var endExclusive = start.AddMonths(1);

        // 行事曆只查一次，所有 users 共用
        var calendarResult = await calendar.GetRequiredDaysAsync(CalendarId, start, endExclusive, ct);
        if (!calendarResult.IsSuccess) return calendarResult.Error!.ToProblem(http);
        var calendarDays = calendarResult.Value!;

        // 取得非 admin users，依員工編號排序（方便 HR 依人拆分）
        var adminRoleId = await db.Roles
            .Where(r => r.Name == AuthRoles.Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        var adminUserIds = adminRoleId is not null
            ? await db.UserRoles.Where(ur => ur.RoleId == adminRoleId).Select(ur => ur.UserId).ToHashSetAsync(ct)
            : [];

        var users = await db.Users
            .Where(u => !adminUserIds.Contains(u.Id))
            .OrderBy(u => u.EmployeeId)
            .Select(u => new { u.Id, u.EmployeeId, u.DisplayName, u.Email })
            .ToListAsync(ct);

        // 一次 bulk load 所有 users 的 WorkDays
        var userIds = users.Select(u => u.Id).ToHashSet();
        var allWorkDays = await db.WorkDays
            .AsNoTracking()
            .Where(d => userIds.Contains(d.UserId) && d.Date >= start && d.Date < endExclusive)
            .Include(d => d.Punches)
            .Include(d => d.AttendanceRequests)
            .ToListAsync(ct);

        var workDaysByUser = allWorkDays
            .GroupBy(d => d.UserId)
            .ToDictionary(g => g.Key, g => g.ToList());

        // 以下開始寫入 response，所有 error return 必須在此行之前
        var fileName = $"timecard-{year:D4}-{month:D2}.csv";
        http.Response.ContentType = "text/csv; charset=utf-8";
        http.Response.Headers["Content-Disposition"] = $"attachment; filename=\"{fileName}\"";

        await using var writer = new StreamWriter(
            http.Response.Body,
            new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            leaveOpen: true);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);

        csv.WriteField("姓名");
        csv.WriteField("年");
        csv.WriteField("日期");
        csv.WriteField("星期");
        csv.WriteField("上班");
        csv.WriteField("下班");
        csv.WriteField("有效工時(分)");
        csv.WriteField("不足(分)");
        csv.WriteField("備註");
        await csv.NextRecordAsync();

        foreach (var user in users)
        {
            var userWorkDays = workDaysByUser.GetValueOrDefault(user.Id) ?? [];
            var computedDays = MonthReportBuilder.Build(userWorkDays, calendarDays, start, endExclusive);
            var displayName = user.DisplayName ?? user.Email ?? user.EmployeeId;

            foreach (var day in computedDays)
            {
                var date = day.Summary.Date;
                csv.WriteField(displayName);
                csv.WriteField(year);
                csv.WriteField(date.ToString("MM/dd"));
                csv.WriteField(ChineseWeekdays[(int)date.DayOfWeek]);
                csv.WriteField(day.PunchStart is not null ? TaiwanTime.ToTime(day.PunchStart.Value).ToString("HH:mm") : "");
                csv.WriteField(day.PunchEnd is not null ? TaiwanTime.ToTime(day.PunchEnd.Value).ToString("HH:mm") : "");
                csv.WriteField(day.Summary.EligibleMinutes);
                csv.WriteField(day.Summary.DeficitMinutes);
                csv.WriteField(day.CalendarDay.Note);
                await csv.NextRecordAsync();
            }
        }

        return Results.Empty;
    }
}
