namespace Timecard.Api.Features.Month;


public sealed record MonthDayDto(
    string Date,
    bool Exists,
    bool IsNonWorkingDay,
    string Note,
    string CalendarKind,
    DateTimeOffset? Start,
    DateTimeOffset? End,
    int PunchCount,
    int PlannedMinutes,
    int PunchedMinutes,
    int EligibleMinutes,
    int EligibleDeltaMinutes,
    int FlexDeltaMinutes,
    int DeficitMinutes
);


/// <param name="AsOf">結算基準日（伺服器端的今天）。前端應以此判斷哪些日期屬於「未來」，確保顯示邏輯與已結算計算使用同一基準。</param>
/// <param name="SettledFlexBankMinutes">截至 <see cref="AsOf"/> 的彈性銀行餘額（僅累計上班日的 FlexDelta）。</param>
/// <param name="SettledDeficitMinutes">截至 <see cref="AsOf"/> 的累計不足時數（≥ 0）。</param>
public sealed record MonthResponse(
    int Year,
    int Month,
    DateOnly AsOf,
    int SettledFlexBankMinutes,
    int SettledDeficitMinutes,
    IReadOnlyList<MonthDayDto> Days
);
