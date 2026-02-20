using Timecard.Api.Domain.Entities.WorkDayAggregate;

namespace Timecard.Api.Domain;

/// <summary>
/// 單日結算所需的事實資料（facts）
/// </summary>
/// <param name="PlannedMinutes">當日應出勤分鐘數（免上班日為 0）</param>
/// <param name="PunchMinutes">打卡區間推算的實際在班分鐘數</param>
/// <param name="EligibleMinutes">由 PunchSpan 和 AttendanceRequests 且落在工作時段內的認列分鐘數</param>
public sealed record DailySettlementFacts(
    DateOnly Date,
    int PlannedMinutes,
    int PunchMinutes,
    int EligibleMinutes
)
{
    /// <summary>
    /// 員工有出勤紀錄時，從 <see cref="WorkDay"/> 推算結算事實。
    /// </summary>
    public static DailySettlementFacts FromWorkday(WorkDay day, bool isWorkingDay)
    {
        ArgumentNullException.ThrowIfNull(day);
        
        var plannedMinutes = isWorkingDay ? FlexTimePolicy.PlannedMinutesPerWorkDay : 0;
        var punchedMinutes = day.DerivePunchTimeRange()?.Duration.TotalMinutes ?? 0;
        var eligibleMinutes = day.CalculateEligibleMinutes();

        return new DailySettlementFacts(day.Date, plannedMinutes, (int)punchedMinutes, eligibleMinutes);
    }
    
    /// <summary>
    /// 員工缺勤（無出勤紀錄）時建立結算事實，打卡與認列分鐘數均為 0。
    /// </summary>
    public static DailySettlementFacts FromAbsence(DateOnly date, bool isWorkingDay)
    {
        var plannedMinutes = isWorkingDay ? FlexTimePolicy.PlannedMinutesPerWorkDay : 0;

        return new DailySettlementFacts(date, plannedMinutes, PunchMinutes: 0, EligibleMinutes: 0);
    }
    
}
