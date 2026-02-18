using Timecard.Api.Domain;
using Xunit;

namespace Timecard.Tests;

public class WorkRulesTests
{
    [Fact]
    public void Workday_PositiveDelta_CapsAt55()
    {
        var planned = WorkRules.PlannedMinutesPerWorkDay; // 540
        var worked = planned + 200;

        var d = WorkRules.ComputeDay(planned, worked, creditedMinutes: 0);
        Assert.Equal(200, d.DeltaMinutes);
        Assert.Equal(55, d.FlexDeltaMinutes);
    }

    [Fact]
    public void Workday_NegativeDelta_NotCapped()
    {
        var planned = WorkRules.PlannedMinutesPerWorkDay;
        var worked = planned - 200;

        var d = WorkRules.ComputeDay(planned, worked, creditedMinutes: 0);
        Assert.Equal(-200, d.DeltaMinutes);
        Assert.Equal(-200, d.FlexDeltaMinutes); // 消耗不限制
    }

    [Fact]
    public void NonWorkingDay_DoesNotAffectFlex()
    {
        var d = WorkRules.ComputeDay(plannedMinutes: 0, workedMinutes: 999, creditedMinutes: 0);
        Assert.Equal(0, d.FlexDeltaMinutes);
    }

    [Fact]
    public void Month_FlexCannotGoBelowZero_DeficitReported()
    {
        // d1(2/1): worked +60 → flexDelta +55 (capped), bank 55
        // d2(2/2): worked -60 → flexDelta -60 (uncapped), need 60, bank 55 → used 55, deficit 5, bank 0
        // d3(2/3): worked -60 → flexDelta -60, need 60, bank 0 → deficit 60, bank 0
        var d1 = new DatedWorkSummary(new DateOnly(2026, 2, 1), WorkRules.ComputeDay(540, 540 + 60, 0));
        var d2 = new DatedWorkSummary(new DateOnly(2026, 2, 2), WorkRules.ComputeDay(540, 540 - 60, 0));
        var d3 = new DatedWorkSummary(new DateOnly(2026, 2, 3), WorkRules.ComputeDay(540, 540 - 60, 0));

        var m = WorkRules.ComputeMonth(new[] { d2, d3, d1 }); // 故意亂序，驗證排序
        Assert.Equal(5, m.Days[1].DeficitMinutes);   // d2: deficit 5
        Assert.Equal(60, m.Days[2].DeficitMinutes);  // d3: deficit 60
        Assert.Equal(0, m.FlexBankBalance);
        Assert.Equal(65, m.TotalDeficitMinutes);
    }

    [Fact]
    public void Month_TotalDeficitMinutes_SumsAllDeficits()
    {
        // 三天都不足，沒有累積
        var d1 = new DatedWorkSummary(new DateOnly(2026, 3, 1), WorkRules.ComputeDay(540, 540 - 30, 0)); // -30
        var d2 = new DatedWorkSummary(new DateOnly(2026, 3, 2), WorkRules.ComputeDay(540, 540 - 20, 0)); // -20
        var d3 = new DatedWorkSummary(new DateOnly(2026, 3, 3), WorkRules.ComputeDay(540, 540 - 10, 0)); // -10

        var m = WorkRules.ComputeMonth(new[] { d1, d2, d3 });
        Assert.Equal(60, m.TotalDeficitMinutes); // 30+20+10
        Assert.Equal(0, m.FlexBankBalance);
    }
}
