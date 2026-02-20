namespace Timecard.Api.Domain;

/// <summary>
/// 整月彈性時數報表：逐日明細、月底餘額、以及累計不足時數。
/// </summary>
/// <param name="Days">逐日明細（依日期排序）。</param>
/// <param name="TotalFlexBankMinutes">月底彈性銀行餘額。</param>
/// <param name="TotalDeficitMinutes">整月累計不足時數（需以請假等方式補足的總分鐘數，≥ 0）。</param>
public sealed record MonthlyFlexReport(
    IReadOnlyList<DailyWorkSummary> Days,
    int TotalFlexBankMinutes,
    int TotalDeficitMinutes
);
