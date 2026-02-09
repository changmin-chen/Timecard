namespace Timecard.Api.Data;

public sealed class WorkDay
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }

    public bool IsNonWorkingDay { get; set; }
    public string Note { get; set; } = "";

    public List<PunchEvent> Punches { get; set; } = [];
    public List<Adjustment> Adjustments { get; set; } = [];
}

public sealed class PunchEvent
{
    public int Id { get; set; }
    public int WorkDayId { get; set; }

    public DateTimeOffset At { get; set; }
    public string Note { get; set; } = "";
}

public sealed class Adjustment
{
    public int Id { get; set; }
    public int WorkDayId { get; set; }

    // e.g. Leave / Trip / Holiday / Typhoon / Manual
    public string Kind { get; set; } = "Manual";

    // +/-
    public int Minutes { get; set; }

    public string Note { get; set; } = "";
}
