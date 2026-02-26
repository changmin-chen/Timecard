namespace Timecard.Api.Domain.Entities.WorkDayAggregate;


public sealed class PunchEvent : BaseEntity<int>
{
    private PunchEvent()
    {
    }

    internal PunchEvent(DateTimeOffset at, string? note)
    {
        if (at.Offset != TimeSpan.Zero)
            throw new ArgumentException("At must be in UTC timezone.");
        
        At = at;
        Note = note?.Trim() ?? "";
    }

    public int WorkDayId { get; private set; }

    public DateTimeOffset At { get; private set; }
    public string Note { get; private set; } = "";
}
