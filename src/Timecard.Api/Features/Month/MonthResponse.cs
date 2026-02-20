namespace Timecard.Api.Features.Month;


public sealed record MonthDayDto(
    string Date,
    bool Exists,
    bool IsNonWorkingDay,
    string Note,
    string CalendarKind,
    string CalendarSource,
    int PunchCount,
    int PlannedMinutes,
    int PunchedMinutes,
    int EligibleMinutes,
    int EligibleDeltaMinutes,
    int FlexDeltaMinutes,
    int FlexBankMinutes,
    int DeficitMinutes
);


public sealed record MonthResponse(
    int Year,
    int Month,
    int TotalFlexBankBalance,
    int TotalDeficitMinutes,
    IReadOnlyList<MonthDayDto> Days
);
