namespace Timecard.Api.Domain;

/// <summary>
/// 單日結算所需的事實資料（facts）：
/// - PlannedMinutes：當日應出勤分鐘數（免上班日為 0）
/// - WorkedMinutes：打卡區間推算的實際在班分鐘數
/// - CreditedMinutes：AttendanceRequest 額外認列分鐘數
/// - FlexEligiblePunchMinutes：僅由 PunchSpan 且落在彈性時段內的可累積分鐘數
/// </summary>
public sealed record DailySettlementFacts(
    int PlannedMinutes,
    int WorkedMinutes,
    int CreditedMinutes,
    int FlexEligiblePunchMinutes
);
