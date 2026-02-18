using Timecard.Api.Domain;

namespace Timecard.Api.Domain.Entities.WorkDayAggregate;

public static class WorkDayMetricsExtensions
{
    private static readonly TimeRange FlexWindow = new(WorkRules.FlexWindowStart, WorkRules.FlexWindowEnd);

    public static (DateTimeOffset? Start, DateTimeOffset? End, int WorkedMinutes) DeriveSpan(this WorkDay day)
    {
        if (day.Punches.Count == 0)
            return (null, null, 0);

        DateTimeOffset? start = null;
        DateTimeOffset? end = null;
        var punchCount = 0;

        foreach (var punch in day.Punches)
        {
            punchCount++;
            if (start is null || punch.At < start.Value) start = punch.At;
            if (end is null || punch.At > end.Value) end = punch.At;
        }

        if (punchCount == 1)
            return (start, null, 0);

        var workedMinutes = (int)Math.Max(0, (end!.Value - start!.Value).TotalMinutes);
        return (start, end, workedMinutes);
    }

    public static int CalculateExtensionMinutes(this WorkDay day)
    {
        var punchSpan = GetPunchSpan(day);
        var allRanges = day.AttendanceRequests.Select(r => r.Range).ToList();

        if (punchSpan is null && allRanges.Count == 0) return 0;
        if (punchSpan is null) return (int)allRanges.Span().TotalMinutes;

        allRanges.Add(punchSpan.Value);

        var totalMinutes = allRanges.Span().TotalMinutes;
        var punchMinutes = punchSpan.Value.Duration.TotalMinutes;

        return (int)(totalMinutes - punchMinutes);
    }

    public static int CalculateFlexEligiblePunchMinutes(this WorkDay day)
    {
        var punchSpan = GetPunchSpan(day);
        if (punchSpan is null) return 0;

        var overlapStart = punchSpan.Value.Start > FlexWindow.Start ? punchSpan.Value.Start : FlexWindow.Start;
        var overlapEnd = punchSpan.Value.End < FlexWindow.End ? punchSpan.Value.End : FlexWindow.End;
        if (overlapEnd <= overlapStart) return 0;

        return (int)(overlapEnd - overlapStart).TotalMinutes;
    }

    public static DailySettlementFacts ToDailySettlementFacts(this WorkDay? day, bool isWorkingDay)
    {
        var plannedMinutes = isWorkingDay ? WorkRules.PlannedMinutesPerWorkDay : 0;

        if (day is null)
            return new DailySettlementFacts(plannedMinutes, WorkedMinutes: 0, CreditedMinutes: 0, FlexEligiblePunchMinutes: 0);

        var workedMinutes = day.DeriveSpan().WorkedMinutes;
        var creditedMinutes = day.CalculateExtensionMinutes();
        var flexEligiblePunchMinutes = day.CalculateFlexEligiblePunchMinutes();

        return new DailySettlementFacts(plannedMinutes, workedMinutes, creditedMinutes, flexEligiblePunchMinutes);
    }

    private static TimeRange? GetPunchSpan(WorkDay day)
    {
        var span = day.DeriveSpan();
        if (span.Start is null || span.End is null)
            return null;

        var start = TimeOnly.FromDateTime(span.Start.Value.LocalDateTime);
        var end = TimeOnly.FromDateTime(span.End.Value.LocalDateTime);

        return end > start ? new TimeRange(start, end) : null;
    }
}
