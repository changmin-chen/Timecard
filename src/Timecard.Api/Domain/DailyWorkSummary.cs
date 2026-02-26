namespace Timecard.Api.Domain;

/// <summary>
/// 單日工時結算：實際工時、與計畫的差額、以及 clamp 後的彈性增減量。
/// </summary>
/// <param name="PlannedMinutes">當日應出勤分鐘數（免上班日為 0）。</param>
/// <param name="PunchedMinutes">打卡區間推算的實際在班分鐘數。</param>
/// <param name="EligibleMinutes">PunchedMinutes + GrantedMinutes，實際被認列的總工時。</param>
/// <param name="FlexDeltaMinutes">累積/消耗皆上限 ±55；可存入（正）／提領（負）彈性銀行的分鐘數；免上班日固定為 0。</param>
/// <param name="DeficitMinutes">即使消耗彈性時數，也不足以抵銷的應出勤分鐘數，非負正數</param>
public readonly record struct DailyWorkSummary(
    DateOnly Date,
    int PlannedMinutes,
    int PunchedMinutes,
    int EligibleMinutes,
    int FlexDeltaMinutes,
    int DeficitMinutes
)
{
    /// <summary>
    /// 出勤分鐘數差額 (正值＝多做、負值＝不足)
    /// </summary>
    public int EligibleDeltaMinutes => EligibleMinutes - PlannedMinutes;
}

public static class DailyWorkSummaryExtensions
{
    extension(IEnumerable<DailyWorkSummary> summaries)
    {
        public int FlexBalanceMinutes(DateOnly? asOf = null) =>
            asOf.HasValue
                ? summaries.Where(s => s.Date <= asOf).Sum(s => s.FlexDeltaMinutes)
                : summaries.Sum(s => s.FlexDeltaMinutes);

        public int DeficitBalanceMinutes(DateOnly? asOf = null) =>
            asOf.HasValue
                ? summaries.Where(s => s.Date <= asOf).Sum(s => s.DeficitMinutes)
                : summaries.Sum(s => s.DeficitMinutes);
    }
}
