using Timecard.Api.Domain;
using Xunit;

namespace Timecard.Tests;

public class WorkRulesTests
{
    private static DailySettlementFacts Facts(int planned, int worked, int credited, int flexEligible)
        => new(planned, worked, credited, flexEligible);

    [Fact]
    public void Workday_PositiveFlex_CapsAt55()
    {
        var planned = WorkRules.PlannedMinutesPerWorkDay;
        var facts = Facts(planned, worked: planned + 200, credited: 0, flexEligible: planned + 200);

        var d = WorkRules.ComputeDay(facts);
        Assert.Equal(200, d.AttendanceDeltaMinutes);
        Assert.Equal(55, d.FlexBankDeltaMinutes);
    }

    [Fact]
    public void Workday_NegativeFlex_CapsAt55()
    {
        var planned = WorkRules.PlannedMinutesPerWorkDay;
        var facts = Facts(planned, worked: planned - 200, credited: 0, flexEligible: planned - 200);

        var d = WorkRules.ComputeDay(facts);
        Assert.Equal(-200, d.AttendanceDeltaMinutes);
        Assert.Equal(-55, d.FlexBankDeltaMinutes); // 單日消耗上限 -55
    }

    [Fact]
    public void NonWorkingDay_DoesNotAffectFlex()
    {
        var d = WorkRules.ComputeDay(Facts(planned: 0, worked: 999, credited: 0, flexEligible: 999));
        Assert.Equal(0, d.FlexBankDeltaMinutes);
    }

    [Fact]
    public void Workday_PunchOutsideWindow_OnlyWindowMinutesAffectFlex()
    {
        var d = WorkRules.ComputeDay(Facts(planned: 540, worked: 600, credited: 0, flexEligible: 570));

        Assert.Equal(60, d.AttendanceDeltaMinutes);
        Assert.Equal(30, d.FlexBankDeltaMinutes);
    }

    [Fact]
    public void Workday_AttendanceOutsideWindow_DoesNotIncreaseFlex()
    {
        var d = WorkRules.ComputeDay(Facts(planned: 540, worked: 605, credited: 0, flexEligible: 570));

        Assert.Equal(65, d.AttendanceDeltaMinutes);
        Assert.Equal(30, d.FlexBankDeltaMinutes);
    }

    [Fact]
    public void Workday_LateArrivalWithAfterWindowPunch_ProducesDeficit30()
    {
        var d = WorkRules.ComputeDay(Facts(planned: 540, worked: 540, credited: 0, flexEligible: 510));
        var m = WorkRules.ComputeMonth([new DatedWorkSummary(new DateOnly(2026, 4, 1), d)]);

        Assert.Equal(-30, d.FlexBankDeltaMinutes);
        Assert.Equal(30, m.TotalDeficitMinutes);
        Assert.Equal(30, m.Days[0].DeficitMinutes);
    }

    [Fact]
    public void Month_FlexCannotGoBelowZero_DeficitReported()
    {
        // d1(2/1): flex +55 (capped), bank 55
        // d2(2/2): flex -55 (capped from -60), used 55, deficit 0, bank 0
        // d3(2/3): flex -55 (capped from -60), used 0, deficit 55, bank 0
        var d1 = new DatedWorkSummary(new DateOnly(2026, 2, 1), WorkRules.ComputeDay(Facts(540, worked: 600, credited: 0, flexEligible: 600)));
        var d2 = new DatedWorkSummary(new DateOnly(2026, 2, 2), WorkRules.ComputeDay(Facts(540, worked: 480, credited: 0, flexEligible: 480)));
        var d3 = new DatedWorkSummary(new DateOnly(2026, 2, 3), WorkRules.ComputeDay(Facts(540, worked: 480, credited: 0, flexEligible: 480)));

        var m = WorkRules.ComputeMonth(new[] { d2, d3, d1 }); // 故意亂序，驗證排序
        Assert.Equal(0, m.Days[1].DeficitMinutes);   // d2: 消耗 55 = bank，deficit 0
        Assert.Equal(55, m.Days[2].DeficitMinutes);  // d3: bank 已空，deficit 55
        Assert.Equal(0, m.FlexBankBalance);
        Assert.Equal(55, m.TotalDeficitMinutes);
    }

    [Fact]
    public void Month_TotalDeficitMinutes_SumsAllDeficits()
    {
        // 三天都不足，沒有累積
        var d1 = new DatedWorkSummary(new DateOnly(2026, 3, 1), WorkRules.ComputeDay(Facts(540, worked: 510, credited: 0, flexEligible: 510))); // -30
        var d2 = new DatedWorkSummary(new DateOnly(2026, 3, 2), WorkRules.ComputeDay(Facts(540, worked: 520, credited: 0, flexEligible: 520))); // -20
        var d3 = new DatedWorkSummary(new DateOnly(2026, 3, 3), WorkRules.ComputeDay(Facts(540, worked: 530, credited: 0, flexEligible: 530))); // -10

        var m = WorkRules.ComputeMonth(new[] { d1, d2, d3 });
        Assert.Equal(60, m.TotalDeficitMinutes); // 30+20+10
        Assert.Equal(0, m.FlexBankBalance);
    }
}
