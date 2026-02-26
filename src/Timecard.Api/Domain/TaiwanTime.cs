namespace Timecard.Api.Domain;

public static class TaiwanTime
{
    public static readonly TimeSpan UtcOffset = TimeSpan.FromHours(8);

    public static DateOnly ToDate(DateTimeOffset value)
        => DateOnly.FromDateTime(value.ToOffset(UtcOffset).DateTime);

    public static TimeOnly ToTime(DateTimeOffset value)
        => TimeOnly.FromTimeSpan(value.ToOffset(UtcOffset).TimeOfDay);
}
