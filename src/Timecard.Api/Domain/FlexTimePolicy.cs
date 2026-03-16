namespace Timecard.Api.Domain;

/// <summary>
/// 工時計算規則：
/// 1) 每天標準工時 9 小時（540 分鐘，含午休）
/// 2) 每日彈性累積上限 +55 分鐘；消耗（不足）則不設上限
/// 3) 彈性累積僅來自 PunchSpan 且只計 07:30~19:00 內的分鐘
/// 4) 彈性時數按月累積，月底重置
/// </summary>
public static class FlexTimePolicy
{
    public const int PlannedMinutesPerWorkDay = 9 * 60;
    public const int DailyFlexCapMinutes = 55;

    /// <summary>
    /// 結算單日工時。
    /// FlexDelta 依「可累積彈性分鐘 - planned」計算，正值上限 +55，負值下限 -55；
    /// 免上班日固定為 0。
    /// </summary>
    public static DailyWorkSummary ComputeDay(DailySettlementFacts facts)
    {
        int eligibleDelta = facts.EligibleMinutes - facts.PlannedMinutes;
        
        bool isNonWorkingDay = facts.PlannedMinutes == 0;
        bool isAbsence = facts.PunchMinutes == 0 && !isNonWorkingDay;
        
        // 1) 免上班日: 不累積也不使用彈性
        // 2) 上班日: 彈性 ±55 分鐘上限
        // 3) 整天缺勤: 不動彈性，全部計為不足
        int flexDelta = (isNonWorkingDay || isAbsence) ? 0 : Math.Clamp(eligibleDelta, -DailyFlexCapMinutes, DailyFlexCapMinutes) ;
        int deficit = Math.Max(0, flexDelta - eligibleDelta);  // 不足時數 = 彈性時數不夠扣的
        
        return new DailyWorkSummary(facts.Date, facts.PlannedMinutes, facts.PunchMinutes, facts.EligibleMinutes, FlexDeltaMinutes: flexDelta, DeficitMinutes: deficit);
    }

    /// <summary>
    /// 按日期順序逐日結算彈性銀行：正彈性存入、負彈性提領（不足部分記為 deficit）。
    /// 回傳月報含逐日明細、月底餘額、以及累計不足時數（需請假補足的總量）。
    /// </summary>
    public static MonthlyFlexReport ComputeMonth(IEnumerable<DailyWorkSummary> daysInAnyOrder)
    {
        var days = daysInAnyOrder.OrderBy(d => d.Date).ToList();
        var totalFlex = days.FlexBalanceMinutes();
        var totalDeficit = days.DeficitBalanceMinutes();
        return new MonthlyFlexReport(days, TotalFlexBankMinutes: totalFlex, TotalDeficitMinutes: totalDeficit);
    }
}
