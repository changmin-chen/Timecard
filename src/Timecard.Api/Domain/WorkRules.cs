namespace Timecard.Api.Domain;

/// <summary>
/// 工時計算規則：
/// 1) 每天標準工時 9 小時（540 分鐘，含午休）
/// 2) 每日彈性累積上限 +55 分鐘；消耗（不足）則不設上限
/// 3) 彈性時數按月累積，月底重置
/// </summary>
public static class WorkRules
{
    public const int PlannedMinutesPerWorkDay = 9 * 60;
    public const int DailyFlexCapMinutes = 55;

    /// <summary>
    /// 結算單日工時。超時累積上限 +55 分鐘，不足則全額認列（無下限）。
    /// 免上班日不累積也不消耗彈性。
    /// </summary>
    public static DailyWorkSummary ComputeDay(int plannedMinutes, int workedMinutes, int creditedMinutes)
    {
        var effective = workedMinutes + creditedMinutes;
        var delta = effective - plannedMinutes;

        // 免上班日：不累積也不使用彈性（避免「放假還賺彈性」）
        // 上班日：正值上限 +55（避免單日過度累積），負值不限（允許全額消耗）
        var flexDelta = plannedMinutes == 0 ? 0 : Math.Min(delta, DailyFlexCapMinutes);

        return new DailyWorkSummary(plannedMinutes, workedMinutes, creditedMinutes, effective, delta, flexDelta);
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

        var delta = d.Summary.FlexDeltaMinutes;
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
