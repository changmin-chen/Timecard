namespace Timecard.Api.Domain;

/// <summary>
/// 單日工時結算：實際工時、與計畫的差額、以及 clamp 後的彈性增減量。
/// </summary>
/// <param name="PlannedMinutes">當日應出勤分鐘數（免上班日為 0）。</param>
/// <param name="WorkedMinutes">打卡區間推算的實際在班分鐘數。</param>
/// <param name="CreditedMinutes">出勤申請（請假／出差等）額外認列的分鐘數。</param>
/// <param name="EffectiveMinutes">WorkedMinutes + CreditedMinutes。</param>
/// <param name="DeltaMinutes">EffectiveMinutes − PlannedMinutes（正值＝多做、負值＝不足）。</param>
/// <param name="FlexDeltaMinutes">Clamp(Delta, ±55) 後可存入／提領彈性銀行的分鐘數；免上班日固定為 0。</param>
public sealed record DailyWorkSummary(
    int PlannedMinutes,
    int WorkedMinutes,
    int CreditedMinutes,
    int EffectiveMinutes,
    int DeltaMinutes,
    int FlexDeltaMinutes
);

/// <summary>帶日期的單日工時結算，作為月結算的輸入。</summary>
public sealed record DatedWorkSummary(DateOnly Date, DailyWorkSummary Summary);
