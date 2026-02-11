namespace Timecard.Api.Data.Entities;

public sealed class AttendanceRequest
{
    private AttendanceRequest()
    {
    }

    internal AttendanceRequest(string category, TimeOnly start, TimeOnly end, string? note)
    {
        Category = category.Trim();
        Start = start;
        End = end;
        Note = note?.Trim() ?? "";
    }

    public int Id { get; private set; }
    public int WorkDayId { get; private set; }

    public string Category { get; private set; } = "Leave";
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }
    public string Note { get; private set; } = "";

    internal void Update(string category, TimeOnly start, TimeOnly end, string? note)
    {
        Category = category.Trim();
        Start = start;
        End = end;
        Note = note?.Trim() ?? "";
    }
}
