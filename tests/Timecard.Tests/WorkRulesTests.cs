using Timecard.Api.Domain;

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
        Assert.Equal(55, d.FlexCandidate);
    }

    [Fact]
    public void Workday_NegativeDelta_CapsAtMinus55()
    {
        var planned = WorkRules.PlannedMinutesPerWorkDay;
        var worked = planned - 200;

        var d = WorkRules.ComputeDay(planned, worked, creditedMinutes: 0);
        Assert.Equal(-200, d.DeltaMinutes);
        Assert.Equal(-55, d.FlexCandidate);
    }

    [Fact]
    public void NonWorkingDay_DoesNotAffectFlex()
    {
        var d = WorkRules.ComputeDay(plannedMinutes: 0, workedMinutes: 999, creditedMinutes: 0);
        Assert.Equal(0, d.FlexCandidate);
    }

    [Fact]
    public void Month_FlexCannotGoBelowZero_DeficitReported()
    {
        var d1 = new DayWithComputed(new DateOnly(2026, 2, 1), WorkRules.ComputeDay(540, 540 + 60, 0)); // +55
        var d2 = new DayWithComputed(new DateOnly(2026, 2, 2), WorkRules.ComputeDay(540, 540 - 60, 0)); // -55 uses bank
        var d3 = new DayWithComputed(new DateOnly(2026, 2, 3), WorkRules.ComputeDay(540, 540 - 60, 0)); // -55 deficit

        var m = WorkRules.ComputeMonth(new[] { d2, d3, d1 });
        Assert.Equal(55, m.Days[2].DeficitMinutes);
        Assert.Equal(0, m.FlexBankEnd);
    }
}
