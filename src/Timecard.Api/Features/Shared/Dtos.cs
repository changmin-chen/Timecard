namespace Timecard.Api.Features.Shared;

public sealed record SessionDto(int Id, DateTimeOffset Start, DateTimeOffset? End);

public sealed record AdjustmentDto(int Id, string Kind, int Minutes, string Note);

public sealed record DayDto(
    string Date,
    bool Exists,
    bool IsNonWorkingDay,
    string Note,
    int PlannedMinutes,
    int WorkedMinutes,
    int CreditedMinutes,
    int EffectiveMinutes,
    int DeltaMinutes,
    int FlexCandidate,
    IReadOnlyList<SessionDto> Sessions,
    IReadOnlyList<AdjustmentDto> Adjustments // yyyy-MM-dd
);

public sealed record MonthDayDto(string Date, bool Exists, bool IsNonWorkingDay, string Note, int PlannedMinutes, int WorkedMinutes, int CreditedMinutes, int EffectiveMinutes, int DeltaMinutes, int FlexCandidate, int FlexApplied, int FlexBankEnd, int DeficitMinutes);

public sealed record MonthDto(int Year, int Month, int FlexBankEnd, IReadOnlyList<MonthDayDto> Days);
