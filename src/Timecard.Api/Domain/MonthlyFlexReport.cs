namespace Timecard.Api.Domain;

/// <summary>
/// 月結算中每日的彈性銀行明細，包含實際提領／存入量與不足時數。
/// </summary>
/// <param name="Date">日期。</param>
/// <param name="Day">當日工時結算。</param>
/// <param name="FlexUsedMinutes">實際存入（正）或提領（負）的彈性分鐘數。</param>
/// <param name="FlexBankBalance">當日結算後的彈性銀行餘額。</param>
/// <param name="DeficitMinutes">彈性不足、需以請假／出差等方式補足的分鐘數（≥ 0）。</param>
public sealed record DailyFlexDetail(
    DateOnly Date,
    DailyWorkSummary Day,
    int FlexUsedMinutes,
    int FlexBankBalance,
    int DeficitMinutes
);

/// <summary>
/// 整月彈性時數報表：逐日明細與月底餘額。
/// </summary>
/// <param name="Days">逐日明細。</param>
/// <param name="FlexBankBalance">月底彈性銀行餘額。</param>
public sealed record MonthlyFlexReport(
    IReadOnlyList<DailyFlexDetail> Days,
    int FlexBankBalance
);
