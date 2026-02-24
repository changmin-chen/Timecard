using Timecard.Api.Domain;

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
            
            var start = TaiwanTime.ToTime(day.Punches.Min(p => p.At));
            var end = TaiwanTime.ToTime(day.Punches.Max(p => p.At));

            return end > start ? new TimeRange(start, end) : null;
        }

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
