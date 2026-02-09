using Timecard.Api.Domain;

namespace Timecard.Tests;

public class WorkRulesTests
{
    [Fact]
    public void Workday_PositiveDelta_CapsAt55()
    {
        var planned = WorkRules.PlannedMinutesPerWorkDay; // 540
        var worked = planned + 200;
        var credited = 0;

        var d = WorkRules.ComputeDay(planned, worked, credited);
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
        // Day1: +55 bank
        var d1 = new DayWithComputed(new DateOnly(2026, 2, 1), WorkRules.ComputeDay(540, 540 + 60, 0));
        // Day2: wants -55 but only 55 bank ok
        var d2 = new DayWithComputed(new DateOnly(2026, 2, 2), WorkRules.ComputeDay(540, 540 - 60, 0));
        // Day3: wants -55 but bank=0 => deficit 55
        var d3 = new DayWithComputed(new DateOnly(2026, 2, 3), WorkRules.ComputeDay(540, 540 - 60, 0));

        var m = WorkRules.ComputeMonth(new[] { d2, d3, d1 }); // unsorted input ok
        Assert.Equal(0, m.Days[0].FlexBankEnd); // after day1+day2 bank should return to 0
        Assert.Equal(0, m.Days[1].FlexBankEnd);
        Assert.Equal(0, m.Days[2].FlexBankEnd);
        Assert.Equal(55, m.Days[2].DeficitMinutes);
    }
}
