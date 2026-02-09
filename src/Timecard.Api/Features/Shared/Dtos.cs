namespace Timecard.Api.Features.Shared;

public sealed record PunchDto(int Id, DateTimeOffset At, string Note);
public sealed record AdjustmentDto(int Id, string Kind, int Minutes, string Note);

public sealed record DayDto(
    string Date, // yyyy-MM-dd
    bool Exists,
    bool IsNonWorkingDay,
    string Note,

    DateTimeOffset? Start, // derived: earliest punch
    DateTimeOffset? End,   // derived: latest punch (if >=2 punches)
    int PunchCount,

    int PlannedMinutes,
    int WorkedMinutes,
    int CreditedMinutes,
    int EffectiveMinutes,
    int DeltaMinutes,
    int FlexCandidate,

    IReadOnlyList<PunchDto> Punches,
    IReadOnlyList<AdjustmentDto> Adjustments
);

public sealed record MonthDayDto(
    string Date,
    bool Exists,
    bool IsNonWorkingDay,
    string Note,

    int PunchCount,

    int PlannedMinutes,
    int WorkedMinutes,
    int CreditedMinutes,
    int EffectiveMinutes,
    int DeltaMinutes,
    int FlexCandidate,

    int FlexApplied,
    int FlexBankEnd,
    int DeficitMinutes
);

public sealed record MonthDto(
    int Year,
    int Month,
    int FlexBankEnd,
    IReadOnlyList<MonthDayDto> Days
);

