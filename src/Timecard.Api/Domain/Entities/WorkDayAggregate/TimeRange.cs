using Timecard.Api.Domain.Results;

namespace Timecard.Api.Domain.Entities.WorkDayAggregate;

public readonly record struct TimeRange  // Value Object
{
    public TimeOnly Start { get; }
    public TimeOnly End { get; }

    public TimeRange(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            throw new ArgumentException("End must be after Start.");
        Start = start;
        End = end;
    }

    public static Result<TimeRange> Create(TimeOnly start, TimeOnly end)
    {
        if (end <= start)
            return Result<TimeRange>.Fail(Errors.WorkDay.StartBeforeEnd);

        return Result<TimeRange>.Ok(new TimeRange(start, end));
    }

    /// <summary>True when two ranges overlap (touching endpoints excluded).</summary>
    public bool Overlaps(TimeRange other) =>
        Start < other.End && other.Start < End;

    /// <summary>True when two ranges overlap or touch at endpoints.</summary>
    public bool OverlapsOrTouches(TimeRange other) =>
        Start <= other.End && other.Start <= End;

    public TimeSpan Duration => End - Start;

    public override string ToString() => $"{Start:HH:mm} ~ {End:HH:mm}";
}
