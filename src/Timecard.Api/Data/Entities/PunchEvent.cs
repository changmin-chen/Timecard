namespace Timecard.Api.Data.Entities;


public sealed class PunchEvent
{
    private PunchEvent()
    {
    }

    internal PunchEvent(DateTimeOffset at, string? note)
    {
        At = at;
        Note = note?.Trim() ?? "";
    }

    public int Id { get; private set; }
    public int WorkDayId { get; private set; }

    public DateTimeOffset At { get; private set; }
    public string Note { get; private set; } = "";

    internal void Update(DateTimeOffset at, string? note)
    {
        At = at;
        Note = note?.Trim() ?? "";
    }
}
