namespace Timecard.Api.Data;

public sealed class WorkDay
{
    public int Id { get; set; }
    public DateOnly Date { get; set; }

    // 節慶假日 / 颱風假 / 免上班日：手動指定（需求說可手動）
    public bool IsNonWorkingDay { get; set; }
    public string Note { get; set; } = "";

    public List<WorkSession> Sessions { get; set; } = [];
    public List<Adjustment> Adjustments { get; set; } = [];
}

public sealed class WorkSession
{
    public int Id { get; set; }
    public int WorkDayId { get; set; }

    // 使用 DateTimeOffset：避免你未來踩到時區/夏令時間坑（就算你覺得不會）
    public DateTimeOffset Start { get; set; }
    public DateTimeOffset? End { get; set; }
}

public sealed class Adjustment
{
    public int Id { get; set; }
    public int WorkDayId { get; set; }

    // e.g. Leave / Trip / Holiday / Typhoon / Manual
    public string Kind { get; set; } = "Manual";

    // 「加分」分鐘：請假/出差通常是 +minutes，用來補足規定工時
    // 也允許負數：你想扣掉什麼就扣掉
    public int Minutes { get; set; }

    public string Note { get; set; } = "";
}
