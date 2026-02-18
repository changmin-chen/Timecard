using Timecard.Api.Domain.Entities.WorkDayAggregate;
using Xunit;

namespace Timecard.Tests;

public class WorkDayAggregateTests
{
    private static readonly TimeSpan Offset = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 2, 1));

    private static WorkDay CreateDay() => new(new DateOnly(2026, 2, 1));

    private static DateTimeOffset At(int hour, int minute = 0)
        => new(2026, 2, 1, hour, minute, 0, Offset);

    [Fact]
    public void AddPunch_SameDateWithinIntervalWithoutForce_Fails()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "start", TimeSpan.FromSeconds(30), force: false);

        var result = day.AddPunch(At(9, 0, 10), "duplicate", TimeSpan.FromSeconds(30), force: false);

        Assert.False(result.IsSuccess);
        Assert.Equal("Too fast. Please wait before creating another punch.", result.Error!.Message);
    }

    [Fact]
    public void AddPunch_DifferentDate_Fails()
    {
        var day = CreateDay();
        var offset2 = TimeZoneInfo.Local.GetUtcOffset(new DateTime(2026, 2, 2));

        var result = day.AddPunch(new DateTimeOffset(2026, 2, 2, 9, 0, 0, offset2), "wrong day", TimeSpan.FromSeconds(30), force: false);

        Assert.False(result.IsSuccess);
        Assert.Equal("Changing punch date is not supported in MVP.", result.Error!.Message);
    }

    [Fact]
    public void DeriveSpan_TwoPunches_ReturnsWorkedMinutes()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);

        var span = day.DeriveSpan();

        Assert.Equal(540, span.WorkedMinutes);
        Assert.NotNull(span.Start);
        Assert.NotNull(span.End);
    }

    // --- AttendanceRequest tests ---

    [Fact]
    public void AddAttendanceRequest_ValidRequest_Succeeds()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);

        var result = day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(18, 0), new TimeOnly(19, 0)), "doctor");

        Assert.True(result.IsSuccess);
        Assert.Single(day.AttendanceRequests);
        Assert.Equal("Leave", result.Value!.Category);
    }

    [Fact]
    public void TimeRange_StartAfterEnd_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            new TimeRange(new TimeOnly(18, 0), new TimeOnly(9, 0)));
    }

    [Fact]
    public void TimeRange_Create_StartAfterOrEqualEnd_Fails()
    {
        var startAfterEnd = TimeRange.Create(new TimeOnly(18, 0), new TimeOnly(9, 0));
        var startEqualsEnd = TimeRange.Create(new TimeOnly(9, 0), new TimeOnly(9, 0));

        Assert.False(startAfterEnd.IsSuccess);
        Assert.False(startEqualsEnd.IsSuccess);
        Assert.Equal("workday.start_before_end", startAfterEnd.Error!.Code);
        Assert.Equal("workday.start_before_end", startEqualsEnd.Error!.Code);
    }

    [Fact]
    public void AddAttendanceRequest_EmptyCategory_Throws()
    {
        var day = CreateDay();

        Assert.Throws<ArgumentException>(() =>
            day.AddAttendanceRequest("", new TimeRange(new TimeOnly(9, 0), new TimeOnly(10, 0)), null));
    }

    [Fact]
    public void AddAttendanceRequest_OverlappingRequests_Fails()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);

        day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(18, 0), new TimeOnly(19, 0)), null);
        var result = day.AddAttendanceRequest("Trip", new TimeRange(new TimeOnly(18, 30), new TimeOnly(20, 0)), null);

        Assert.False(result.IsSuccess);
        Assert.Equal("Attendance request overlaps with an existing one.", result.Error!.Message);
    }

    [Fact]
    public void AddAttendanceRequest_GapBetweenPunchAndRequest_Fails()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);

        // Gap between 18:00 and 19:00
        var result = day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(19, 0), new TimeOnly(20, 0)), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("gap", result.Error!.Message);
    }

    [Fact]
    public void AddAttendanceRequest_ContiguousBeforePunch_Succeeds()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);

        var result = day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(7, 0), new TimeOnly(9, 0)), null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddAttendanceRequest_ContiguousAfterPunch_Succeeds()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);

        var result = day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(18, 0), new TimeOnly(20, 0)), null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddAttendanceRequest_NoPunches_SingleRequest_Succeeds()
    {
        var day = CreateDay();

        var result = day.AddAttendanceRequest("Holiday", new TimeRange(new TimeOnly(9, 0), new TimeOnly(18, 0)), "national holiday");

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddAttendanceRequest_NoPunches_TwoContiguousRequests_Succeeds()
    {
        var day = CreateDay();
        day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(9, 0), new TimeOnly(13, 0)), null);

        var result = day.AddAttendanceRequest("Trip", new TimeRange(new TimeOnly(13, 0), new TimeOnly(18, 0)), null);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void AddAttendanceRequest_NoPunches_TwoNonContiguousRequests_Fails()
    {
        var day = CreateDay();
        day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(9, 0), new TimeOnly(12, 0)), null);

        var result = day.AddAttendanceRequest("Trip", new TimeRange(new TimeOnly(14, 0), new TimeOnly(18, 0)), null);

        Assert.False(result.IsSuccess);
        Assert.Contains("gap", result.Error!.Message);
    }

    // --- CalculateExtensionMinutes tests ---

    [Fact]
    public void CalculateExtensionMinutes_NoPunchesNoRequests_ReturnsZero()
    {
        var day = CreateDay();

        Assert.Equal(0, day.CalculateExtensionMinutes());
    }

    [Fact]
    public void CalculateExtensionMinutes_OnlyPunchesNoRequests_ReturnsZero()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);

        Assert.Equal(0, day.CalculateExtensionMinutes());
    }

    [Fact]
    public void CalculateExtensionMinutes_ExtensionBeforePunch()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);
        day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(7, 0), new TimeOnly(9, 0)), null);

        // effectiveStart=7:00, effectiveEnd=18:00, totalSpan=660, punchSpan=540, extension=120
        Assert.Equal(120, day.CalculateExtensionMinutes());
    }

    [Fact]
    public void CalculateExtensionMinutes_ExtensionAfterPunch()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);
        day.AddAttendanceRequest("Trip", new TimeRange(new TimeOnly(18, 0), new TimeOnly(20, 0)), null);

        // effectiveStart=9:00, effectiveEnd=20:00, totalSpan=660, punchSpan=540, extension=120
        Assert.Equal(120, day.CalculateExtensionMinutes());
    }

    [Fact]
    public void CalculateExtensionMinutes_ExtensionBothSides()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        day.AddPunch(At(18, 0), "", TimeSpan.Zero, force: true);
        day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(7, 0), new TimeOnly(9, 0)), null);
        day.AddAttendanceRequest("Trip", new TimeRange(new TimeOnly(18, 0), new TimeOnly(20, 0)), null);

        // effectiveStart=7:00, effectiveEnd=20:00, totalSpan=780, punchSpan=540, extension=240
        Assert.Equal(240, day.CalculateExtensionMinutes());
    }

    [Fact]
    public void CalculateExtensionMinutes_NoPunches_OnlyRequests()
    {
        var day = CreateDay();
        day.AddAttendanceRequest("Holiday", new TimeRange(new TimeOnly(9, 0), new TimeOnly(18, 0)), null);

        // No punch span, entire range = 540 minutes
        Assert.Equal(540, day.CalculateExtensionMinutes());
    }

    [Fact]
    public void CalculateExtensionMinutes_OnePunch_WithRequest()
    {
        var day = CreateDay();
        day.AddPunch(At(9, 0), "", TimeSpan.Zero, force: true);
        // Only 1 punch => no punch span, but request exists
        day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(9, 0), new TimeOnly(18, 0)), null);

        // hasPunchSpan=false, hasRequests=true => extension = 540
        Assert.Equal(540, day.CalculateExtensionMinutes());
    }

    [Fact]
    public void RemoveAttendanceRequest_Succeeds()
    {
        var day = CreateDay();
        day.AddAttendanceRequest("Leave", new TimeRange(new TimeOnly(9, 0), new TimeOnly(18, 0)), null);

        // Since Id is private set and defaults to 0 in-memory, we use 0
        var result = day.RemoveAttendanceRequest(0);

        Assert.True(result.IsSuccess);
        Assert.Empty(day.AttendanceRequests);
    }

    [Fact]
    public void RemoveAttendanceRequest_NotFound_Fails()
    {
        var day = CreateDay();

        var result = day.RemoveAttendanceRequest(999);

        Assert.False(result.IsSuccess);
        Assert.Equal("Attendance request not found.", result.Error!.Message);
    }

    // Helper to create DateTimeOffset with seconds
    private static DateTimeOffset At(int hour, int minute, int second)
        => new(2026, 2, 1, hour, minute, second, Offset);
}
