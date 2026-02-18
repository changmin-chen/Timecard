using Timecard.Api.Domain;

namespace Timecard.Api.Domain.Entities.WorkDayAggregate;

public static class WorkDayMetricsExtensions
{
    private static readonly TimeRange FlexWindow = new(WorkRules.FlexWindowStart, WorkRules.FlexWindowEnd);

    extension(WorkDay day)
    {
        public (DateTimeOffset? Start, DateTimeOffset? End, int PunchedMinutes) DeriveSpan()
        {
            if (day.Punches.Count == 0)
                return (null, null, 0);

            var start = day.Punches.Min(p => p.At);
            var end = day.Punches.Max(p => p.At);

            if (day.Punches.Count == 1)
                return (start, null, 0);

            var punchedMinutes = (int)(end - start).TotalMinutes;
            return (start, end, punchedMinutes);
        }
        
        public int CalculateGrantedMinutes()
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
        
        public int CalculateFlexEligiblePunchMinutes()
        {
            var punchSpan = GetPunchSpan(day);
            if (punchSpan is null) return 0;

            var intersection = punchSpan.Value.TryIntersect(FlexWindow);
            return intersection is null ? 0 : (int)intersection.Value.Duration.TotalMinutes;
        }
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
