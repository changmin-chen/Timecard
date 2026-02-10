namespace Timecard.Api.Data.Entities;

public sealed class Adjustment
{
    private Adjustment()
    {
    }

    internal Adjustment(string kind, int minutes, string? note)
    {
        Kind = kind.Trim();
        Minutes = minutes;
        Note = note?.Trim() ?? "";
    }

    public int Id { get; private set; }
    public int WorkDayId { get; private set; }

    public string Kind { get; private set; } = "Manual";
    public int Minutes { get; private set; }
    public string Note { get; private set; } = "";

    internal void Update(string kind, int minutes, string? note)
    {
        Kind = kind.Trim();
        Minutes = minutes;
        Note = note?.Trim() ?? "";
    }
}
