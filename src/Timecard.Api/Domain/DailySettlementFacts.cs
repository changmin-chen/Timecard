using Timecard.Api.Domain.Entities.WorkDayAggregate;

namespace Timecard.Api.Domain;

/// <summary>
/// 單日結算所需的事實資料（facts）
/// </summary>
/// <param name="PlannedMinutes">當日應出勤分鐘數（免上班日為 0）</param>
/// <param name="WorkedMinutes">打卡區間推算的實際在班分鐘數</param>
/// <param name="GrantedMinutes"> <see cref="AttendanceRequest"/> 額外認列分鐘數</param>
/// <param name="FlexEligiblePunchMinutes">僅由 PunchSpan 且落在彈性時段內的可累積分鐘數</param>
public sealed record DailySettlementFacts(
    int PlannedMinutes,
    int WorkedMinutes,
    int GrantedMinutes,
    int FlexEligiblePunchMinutes
)
{
    /// <summary>
    /// Derives a <see cref="DailySettlementFacts"/> from a <see cref="WorkDay"/>.
    /// Pass <see langword="null"/> for <paramref name="day"/> when the employee was absent.
    /// </summary>
    public static DailySettlementFacts From(WorkDay? day, bool isWorkingDay)
    {
        var plannedMinutes = isWorkingDay ? WorkRules.PlannedMinutesPerWorkDay : 0;

        if (day is null)
            return new DailySettlementFacts(plannedMinutes, WorkedMinutes: 0, GrantedMinutes: 0, FlexEligiblePunchMinutes: 0);

        var punchedMinutes = day.DeriveSpan().PunchedMinutes;
        var grantedMinutes = day.CalculateGrantedMinutes();
        var flexEligiblePunchMinutes = day.CalculateFlexEligiblePunchMinutes();

        return new DailySettlementFacts(plannedMinutes, punchedMinutes, grantedMinutes, flexEligiblePunchMinutes);
    }
}
