using Timecard.Api.Domain.Results;

namespace Timecard.Api.Domain.Entities.WorkDayAggregate;

/// <summary>
/// Value Object representing a time range. End should always be after and not touching Start.
/// </summary>
public readonly record struct TimeRange
{
    public TimeOnly Start { get; } 
    public TimeOnly End { get; }
    
    public TimeSpan Duration => End - Start;
    

    public TimeRange(TimeOnly start, TimeOnly end)
    {
        if (end <= start) throw new ArgumentException("End must be after Start.");
        
        Start = start;
        End = end;
    }

    /// <summary>True when two ranges overlap (touching endpoints excluded).</summary>
    public bool Overlaps(TimeRange other) =>
        Start < other.End && other.Start < End;

    /// <summary>True when two ranges overlap or touch at endpoints.</summary>
    public bool OverlapsOrTouches(TimeRange other) =>
        Start <= other.End && other.Start <= End;

    /// <summary>
    /// True when there is a gap between this range and <paramref name="other"/>
    /// (i.e., they neither overlap nor touch).
    /// </summary>
    public bool HasGapBetween(TimeRange other) => !OverlapsOrTouches(other);


    /// <summary>
    /// Returns the gap <see cref="TimeRange"/> between this range and <paramref name="other"/>,
    /// or <see langword="null"/> when no gap exists (they overlap or touch).
    /// The returned range always spans from the earlier end-point to the later start-point,
    /// regardless of which range comes first.
    /// </summary>
    /// <example>
    /// [08:00~09:00].TryGetGap([10:00~11:00]) → TimeRange(09:00, 10:00)
    /// [08:00~09:00].TryGetGap([09:00~10:00]) → null
    /// </example>
    public TimeRange? TryGetGap(TimeRange other)
    {
        // Determine the "gap window": earlier end → later start
        TimeOnly gapStart = End < other.End ? End : other.End; // min(End, other.End)
        TimeOnly gapEnd = Start > other.Start ? Start : other.Start; // max(Start, other.Start)

        return gapEnd > gapStart
            ? new TimeRange(gapStart, gapEnd)
            : null;
    }
    
    
    public override string ToString() => $"{Start:HH:mm} ~ {End:HH:mm}";
}

public static class TimeRangeCreation
{
    extension(TimeRange)
    {
        public static Result<TimeRange> Create(TimeOnly start, TimeOnly end) =>
            end > start ? new TimeRange(start, end) : Errors.WorkDay.StartBeforeEnd;
    }
}

public static class TimeRangeCollectionExtensions
{
    extension(IEnumerable<TimeRange> ranges)
    {
        /// <summary>
        /// Returns the total <see cref="TimeSpan"/> from the earliest Start
        /// to the latest End across all ranges.
        /// </summary>
        public TimeSpan Span()
        {
            var list = ranges as IList<TimeRange> ?? ranges.ToList();
            if (list.Count == 0) throw new InvalidOperationException("Cannot compute span of an empty collection.");

            return list.Max(r => r.End) - list.Min(r => r.Start);
        }
        
        /// <summary>
        /// Returns true if there is at least one gap between any two segments.
        /// </summary>
        public bool HasGaps()
        {
            using var sorted = ranges.OrderBy(s => s.Start).GetEnumerator();
            if (!sorted.MoveNext()) return false;

            var maxEnd = sorted.Current.End;
            while (sorted.MoveNext())
            {
                if (sorted.Current.Start > maxEnd) return true;
                if (sorted.Current.End > maxEnd) maxEnd = sorted.Current.End;
            }

            return false;
        }
    }
}
