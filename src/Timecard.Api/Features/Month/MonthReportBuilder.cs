using Timecard.Api.Domain;
using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Timecard.Api.Features.Calendar;

namespace Timecard.Api.Features.Month;

/// <summary>
/// 單日計算結果（計算層，非 DTO）。各 endpoint 各自決定要如何映射成輸出格式。
/// </summary>
internal sealed record ComputedDay(
    DailyWorkSummary Summary,
    ResolvedCalendarDay CalendarDay,
    DateTimeOffset? PunchStart,
    DateTimeOffset? PunchEnd,
    WorkDay? Source
);

internal static class MonthReportBuilder
{
    /// <summary>
    /// 計算 [start, endExclusive) 內每一天的工時結算，依日期排序。
    /// 無打卡紀錄的日期仍會出現（以 FromAbsence 計算）。
    /// </summary>
    internal static IReadOnlyList<ComputedDay> Build(
        IEnumerable<WorkDay> workDays,
        IReadOnlyDictionary<DateOnly, ResolvedCalendarDay> calendarDays,
        DateOnly start,
        DateOnly endExclusive)
    {
        var workDayMap = workDays.ToDictionary(d => d.Date);

        return Enumerable
            .Range(0, endExclusive.DayNumber - start.DayNumber)
            .Select(i => start.AddDays(i))
            .Select(date =>
            {
                workDayMap.TryGetValue(date, out var day);
                var calDay = calendarDays[date];
                var facts = day is not null
                    ? DailySettlementFacts.FromWorkday(day, calDay.IsWorking)
                    : DailySettlementFacts.FromAbsence(date, calDay.IsWorking);
                var summary = FlexTimePolicy.ComputeDay(facts);
                var (punchStart, punchEnd) = day?.GetPunchTimestamps() ?? (null, null);
                return new ComputedDay(summary, calDay, punchStart, punchEnd, day);
            })
            .ToList();
    }
}
