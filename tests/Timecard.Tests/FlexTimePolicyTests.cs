using Timecard.Api.Domain;
using Xunit;

namespace Timecard.Tests;

public class FlexTimePolicyTests
{
    private static DailySettlementFacts Facts(int planned, int punch, int eligible, DateOnly? date = null)
    {
        date ??= new DateOnly(2026, 2, 1);
        return new DailySettlementFacts(date.Value, planned, punch, eligible);
    }

    [Fact]
    public void Workday_PositiveFlex_CapsAt55()
    {
        var planned = FlexTimePolicy.PlannedMinutesPerWorkDay;
        var facts = Facts(planned, punch: planned + 200, eligible: planned + 200);

        var d = FlexTimePolicy.ComputeDay(facts);
        Assert.Equal(200, d.AttendanceDeltaMinutes);
        Assert.Equal(55, d.FlexDeltaMinutes);
    }

    [Fact]
    public void Workday_NegativeFlex_CapsAt55()
    {
        var planned = FlexTimePolicy.PlannedMinutesPerWorkDay;
        var facts = Facts(planned, punch: planned - 200, eligible: planned - 200);

        var d = FlexTimePolicy.ComputeDay(facts);
        Assert.Equal(-200, d.AttendanceDeltaMinutes);
        Assert.Equal(-55, d.FlexDeltaMinutes); // 單日消耗上限 -55
    }

    [Fact]
    public void Workday_SufficientEligible_NoDeficit()
    {
        var planned = FlexTimePolicy.PlannedMinutesPerWorkDay;
        var facts = Facts(planned, punch: planned + 200, eligible: planned + 200);
        
        var d = FlexTimePolicy.ComputeDay(facts);
        Assert.True(d.DeficitMinutes == 0);
    }

    [Fact]
    public void Workday_InsufficientEligibleMoreThanFlexCap_HasDeficit()
    {
        var planned = FlexTimePolicy.PlannedMinutesPerWorkDay;
        var flexCap = FlexTimePolicy.DailyFlexCapMinutes;
        const int expectedDeficit = 20;
        
        var facts = Facts(planned, punch: planned - flexCap - expectedDeficit, eligible: planned - flexCap - expectedDeficit);
        
        var d = FlexTimePolicy.ComputeDay(facts);
        Assert.Equal(expectedDeficit, d.DeficitMinutes);
    }

    [Fact]
    public void NonWorkingDay_DoesNotAffectFlexAndDeficit()
    {
        var d = FlexTimePolicy.ComputeDay(Facts(planned: 0, punch: 999, eligible: 999));
        Assert.Equal(0, d.FlexDeltaMinutes);
        Assert.Equal(0, d.DeficitMinutes);
    }
    

    [Fact]
    public void Month_BankCanGoBelowZero_DeficitReported()
    {
        // d1(2/1): flex +55 (capped), bank 55
        // d2(2/2): flex -55 (capped from -60), deficit 5, bank 0
        // d3(2/3): flex -55 (capped from -120), deficit 65, bank -55
        var d1 = FlexTimePolicy.ComputeDay(Facts(540, punch: 600, eligible: 600, new DateOnly(2026, 2, 1)));
        var d2 = FlexTimePolicy.ComputeDay(Facts(540, punch: 480, eligible: 480, new DateOnly(2026, 2, 2)));
        var d3 = FlexTimePolicy.ComputeDay(Facts(540, punch: 420, eligible: 420, new DateOnly(2026, 2, 3)));

        var m = FlexTimePolicy.ComputeMonth([d2, d3, d1]); // 故意亂序，驗證排序
        Assert.Equal(5, m.Days[1].Summary.DeficitMinutes);   // d2: 消耗 55 = bank，超額 deficit 5
        Assert.Equal(65, m.Days[2].Summary.DeficitMinutes);  // d3: bank -55，deficit 65
        Assert.Equal(-55, m.TotalFlexBankMinutes);
        Assert.Equal(70, m.TotalDeficitMinutes);
    }
}
