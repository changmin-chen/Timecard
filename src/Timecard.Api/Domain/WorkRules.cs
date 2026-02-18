namespace Timecard.Api.Domain;

/// <summary>
/// 工時計算規則：
/// 1) 每天標準工時 9 小時（540 分鐘，含午休）
/// 2) 每日彈性累積上限 +55 分鐘；消耗（不足）則不設上限
/// 3) 彈性累積僅來自 PunchSpan 且只計 07:30~19:00 內的分鐘
/// 4) 彈性時數按月累積，月底重置
/// </summary>
public static class WorkRules
{
    public const int PlannedMinutesPerWorkDay = 9 * 60;
    public const int DailyFlexCapMinutes = 55;
    public static readonly TimeOnly FlexWindowStart = new(7, 30);
    public static readonly TimeOnly FlexWindowEnd = new(19, 0);

    /// <summary>
    /// 結算單日工時。
    /// FlexDelta 依「可累積彈性分鐘 - planned」計算，正值上限 +55，負值下限 -55；
    /// 免上班日固定為 0。
    /// </summary>
    public static DailyWorkSummary ComputeDay(DailySettlementFacts facts)
    {
        var recognized = facts.WorkedMinutes + facts.GrantedMinutes;
        var attendanceDelta = recognized - facts.PlannedMinutes;
        var flexCandidate = facts.FlexEligiblePunchMinutes - facts.PlannedMinutes;

        // 免上班日：不累積也不使用彈性（避免「放假還賺彈性」）
        // 上班日：±55 分鐘上限（累積與單日消耗皆受限，避免暴衝或一次大量提領）
        var flexDelta = facts.PlannedMinutes == 0 ? 0 : Math.Clamp(flexCandidate, -DailyFlexCapMinutes, DailyFlexCapMinutes);

        return new DailyWorkSummary(facts.PlannedMinutes, facts.WorkedMinutes, facts.GrantedMinutes, recognized, attendanceDelta, flexDelta);
    }

    /// <summary>
    /// 按日期順序逐日結算彈性銀行：正彈性存入、負彈性提領（不足部分記為 deficit）。
    /// 回傳月報含逐日明細、月底餘額、以及累計不足時數（需請假補足的總量）。
    /// </summary>
    public static MonthlyFlexReport ComputeMonth(IEnumerable<DatedWorkSummary> daysInAnyOrder)
    {
        var days = daysInAnyOrder.OrderBy(d => d.Date).ToList();
        var bank = 0;
        var results = days.Select(d => AccumulateFlex(d, ref bank)).ToList();
        var totalDeficit = results.Sum(d => d.DeficitMinutes);
        return new MonthlyFlexReport(results, FlexBankBalance: bank, TotalDeficitMinutes: totalDeficit);
    }

    /// <summary>處理單日的彈性存提，更新銀行餘額。</summary>
    private static DailyFlexDetail AccumulateFlex(DatedWorkSummary d, ref int bank)
    {
        if (d.Summary.PlannedMinutes == 0)
            return new DailyFlexDetail(d.Date, d.Summary, FlexUsedMinutes: 0, FlexBankBalance: bank, DeficitMinutes: 0);

        var delta = d.Summary.FlexBankDeltaMinutes;
        if (delta >= 0)
        {
            bank += delta;
            return new DailyFlexDetail(d.Date, d.Summary, FlexUsedMinutes: delta, FlexBankBalance: bank, DeficitMinutes: 0);
        }
        
        var need = -delta;
        var used = Math.Min(bank, need);
        bank -= used;
        return new DailyFlexDetail(d.Date, d.Summary, FlexUsedMinutes: -used, FlexBankBalance: bank, DeficitMinutes: need - used);
    }
}
