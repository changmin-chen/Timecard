using Timecard.Api.Data.Entities;
using Xunit;

namespace Timecard.Tests;

public class WorkDayAggregateTests
{
    [Fact]
    public void AddPunch_SameDateWithinIntervalWithoutForce_Fails()
    {
        var date = new DateOnly(2026, 2, 1);
        var offset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 2, 1));
        var day = new WorkDay(date);

        day.AddPunch(new DateTimeOffset(2026, 2, 1, 9, 0, 0, offset), "start", TimeSpan.FromSeconds(30), force: false);

        var result = day.AddPunch(new DateTimeOffset(2026, 2, 1, 9, 0, 10, offset), "duplicate", TimeSpan.FromSeconds(30), force: false);

        Assert.False(result.IsSuccess);
        Assert.Equal("Too fast. Please wait before creating another punch.", result.Error!.Message);
    }

    [Fact]
    public void AddPunch_DifferentDate_Fails()
    {
        var date = new DateOnly(2026, 2, 1);
        var offset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 2, 2));
        var day = new WorkDay(date);

        var result = day.AddPunch(new DateTimeOffset(2026, 2, 2, 9, 0, 0, offset), "wrong day", TimeSpan.FromSeconds(30), force: false);

        Assert.False(result.IsSuccess);
        Assert.Equal("Changing punch date is not supported in MVP.", result.Error!.Message);
    }

    [Fact]
    public void DeriveSpan_TwoPunches_ReturnsWorkedMinutes()
    {
        var date = new DateOnly(2026, 2, 1);
        var offset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 2, 1));
        var day = new WorkDay(date);

        day.AddPunch(new DateTimeOffset(2026, 2, 1, 9, 0, 0, offset), "", TimeSpan.Zero, force: true);
        day.AddPunch(new DateTimeOffset(2026, 2, 1, 18, 0, 0, offset), "", TimeSpan.Zero, force: true);

        var span = day.DeriveSpan();

        Assert.Equal(540, span.WorkedMinutes);
        Assert.NotNull(span.Start);
        Assert.NotNull(span.End);
    }

    [Fact]
    public void AddAdjustment_MultipleEntries_SumsCreditedMinutes()
    {
        var day = new WorkDay(new DateOnly(2026, 2, 1));

        day.AddAdjustment("Manual", 30, "");
        day.AddAdjustment("Leave", -10, "");

        Assert.Equal(20, day.CreditedMinutes);
    }
}
