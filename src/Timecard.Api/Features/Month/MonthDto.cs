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
    int WorkedMinutes,
    int ExtensionMinutes,
    int EffectiveMinutes,
    int DeltaMinutes,
    int FlexDeltaMinutes,

    int FlexUsedMinutes,
    int FlexBankBalance,
    int DeficitMinutes
);


public sealed record MonthDto(
    int Year,
    int Month,
    int FlexBankBalance,
    IReadOnlyList<MonthDayDto> Days
);
