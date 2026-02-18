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
    int GrantedMinutes,
    int RecognizedMinutes,
    int AttendanceDeltaMinutes,
    int FlexBankDeltaMinutes,

    int FlexUsedMinutes,
    int FlexBankBalance,
    int DeficitMinutes
);


public sealed record MonthDto(
    int Year,
    int Month,
    int FlexBankBalance,
    int TotalDeficitMinutes,
    IReadOnlyList<MonthDayDto> Days
);
