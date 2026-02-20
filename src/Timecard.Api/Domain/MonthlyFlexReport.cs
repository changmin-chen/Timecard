namespace Timecard.Api.Domain;

/// <summary>
/// 月結算中每日的彈性銀行明細，包含實際提領／存入量與不足時數。
/// </summary>
/// <param name="Summary">當日工時結算。</param>
/// <param name="FlexBankMinutes">當日結算後的彈性銀行餘額。</param>
public sealed record DailyFlexReport(
    DailyWorkSummary Summary,
    int FlexBankMinutes
)
{
    public DateOnly Date => Summary.Date;
}


/// <summary>
/// 整月彈性時數報表：逐日明細、月底餘額、以及累計不足時數。
/// </summary>
/// <param name="Days">逐日明細。</param>
/// <param name="TotalFlexBankMinutes">月底彈性銀行餘額。</param>
/// <param name="TotalDeficitMinutes">整月累計不足時數（需以請假等方式補足的總分鐘數，≥ 0）。</param>
public sealed record MonthlyFlexReport(
    IReadOnlyList<DailyFlexReport> Days,
    int TotalFlexBankMinutes,
    int TotalDeficitMinutes
);
