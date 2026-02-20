namespace Timecard.Api.Domain.Entities.WorkDayAggregate;

public static class WorkDayMetricsExtensions
{
    private static readonly TimeOnly EligibleWindowStart = new(7, 30);
    private static readonly TimeOnly EligibleWindowEnd = new(19, 0);
    
    private static readonly TimeRange EligibleWindow = new(EligibleWindowStart, EligibleWindowEnd);

    extension(WorkDay day)
    {
        public (DateTimeOffset? Start, DateTimeOffset? End) GetPunchTimestamps()
        {
            if (day.Punches.Count == 0)
                return (null, null);
        
            var start = day.Punches.Min(p => p.At);
            var end = day.Punches.Max(p => p.At);
        
            if (day.Punches.Count == 1)
                return (start, null);
        
            return (start, end);
        }

        public TimeRange? DerivePunchTimeRange()
        {
            if (day.Punches.Count < 2) return null;
            
            var start = TimeOnly.FromDateTime(day.Punches.Min(p => p.At).LocalDateTime);
            var end = TimeOnly.FromDateTime(day.Punches.Max(p => p.At).LocalDateTime);

            return end > start ? new TimeRange(start, end) : null;
        }

        // [Obsolete("中間計算產物，沒人在乎Granted，不如直接CalculateFlexEligibleMinutes")]
        // public int CalculateGrantedMinutes()
        // {
        //     var punchSpan = day.DerivePunchTimeRange();
        //     var allRanges = day.AttendanceRequests.Select(r => r.Range).ToList();
        //
        //     if (punchSpan is null && allRanges.Count == 0) return 0;
        //     if (punchSpan is null) return (int)allRanges.LongestSpan().TotalMinutes;
        //
        //     allRanges.Add(punchSpan.Value);
        //
        //     var totalMinutes = allRanges.LongestSpan().TotalMinutes;
        //     var punchMinutes = punchSpan.Value.Duration.TotalMinutes;
        //
        //     return (int)(totalMinutes - punchMinutes);
        // }

        // [Obsolete("Use CalculateFlexEligibleMinutes instead.")]
        // public int CalculateFlexEligiblePunchMinutes()
        // {
        //     var punchSpan = day.DerivePunchTimeRange();
        //     if (punchSpan is null) return 0;
        //
        //     var intersection = punchSpan.Value.TryIntersect(FlexWindow);
        //     return intersection is null ? 0 : (int)intersection.Value.Duration.TotalMinutes;
        // }

        public int CalculateEligibleMinutes()
        {
            var worked = day.DerivePunchTimeRange();
            var allRanges = day.AttendanceRequests.Select(r => r.Range).ToList();

            if (worked is null && allRanges.Count == 0) return 0;
            if (worked is not null) allRanges.Add(worked.Value);

            var eligibleRanges = allRanges
                .Select(r => r.TryIntersect(EligibleWindow))
                .Where(r => r.HasValue)
                .Select(r => r!.Value);

            return (int)eligibleRanges.TotalDistinctDuration().TotalMinutes;
        }
    }
}
