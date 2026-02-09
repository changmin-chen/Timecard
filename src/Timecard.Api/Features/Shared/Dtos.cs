namespace Timecard.Api.Features.Shared;

public sealed record SessionDto(int id, DateTimeOffset start, DateTimeOffset? end);
public sealed record AdjustmentDto(int id, string kind, int minutes, string note);

public sealed record DayDto(
    string date, // yyyy-MM-dd
    bool exists,
    bool isNonWorkingDay,
    string note,

    int plannedMinutes,
    int workedMinutes,
    int creditedMinutes,
    int effectiveMinutes,
    int deltaMinutes,
    int flexCandidate,

    IReadOnlyList<SessionDto> sessions,
    IReadOnlyList<AdjustmentDto> adjustments
);

public sealed record MonthDayDto(
    string date,
    bool exists,
    bool isNonWorkingDay,
    string note,

    int plannedMinutes,
    int workedMinutes,
    int creditedMinutes,
    int effectiveMinutes,
    int deltaMinutes,
    int flexCandidate,

    int flexApplied,
    int flexBankEnd,
    int deficitMinutes
);

public sealed record MonthDto(
    int year,
    int month,
    int flexBankEnd,
    IReadOnlyList<MonthDayDto> days
);
