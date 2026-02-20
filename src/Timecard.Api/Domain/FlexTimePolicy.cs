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
        int attendanceDelta = facts.EligibleMinutes - facts.PlannedMinutes;

        // 免上班日：不累積也不使用彈性（避免「放假還賺彈性」）
        // 上班日：±55 分鐘上限（累積與單日消耗皆受限，避免暴衝或一次大量提領）
        bool isWorkingDay = facts.PlannedMinutes == 0;
        int flexDelta = isWorkingDay ? 0 : Math.Clamp(attendanceDelta, -DailyFlexCapMinutes, DailyFlexCapMinutes);

        // 彈性時數不夠扣的不足時數
        int deficit = Math.Max(0, -(attendanceDelta - flexDelta));
        return new DailyWorkSummary(facts.Date, facts.PlannedMinutes, facts.PunchMinutes, facts.EligibleMinutes, FlexDeltaMinutes: flexDelta, DeficitMinutes: deficit);
    }

    /// <summary>
    /// 按日期順序逐日結算彈性銀行：正彈性存入、負彈性提領（不足部分記為 deficit）。
    /// 回傳月報含逐日明細、月底餘額、以及累計不足時數（需請假補足的總量）。
    /// </summary>
    public static MonthlyFlexReport ComputeMonth(IEnumerable<DailyWorkSummary> daysInAnyOrder)
    {
        var days = daysInAnyOrder.OrderBy(d => d.Date).ToList();
        var bank = 0;
        var results = days.Select(d => AccumulateFlex(d, ref bank)).ToList();
        var totalDeficit = results.Sum(d => d.Summary.DeficitMinutes);
        return new MonthlyFlexReport(results, TotalFlexBankMinutes: bank, TotalDeficitMinutes: totalDeficit);
    }

    /// <summary>處理單日的彈性存提，更新銀行餘額，餘額可為負值。</summary>
    private static DailyFlexReport AccumulateFlex(DailyWorkSummary d, ref int flexBank)
    {
        if (d.PlannedMinutes == 0)
            return new DailyFlexReport(d, FlexBankMinutes: flexBank);

        flexBank += d.FlexDeltaMinutes;

        return new DailyFlexReport(d, FlexBankMinutes: flexBank);
    }
}
