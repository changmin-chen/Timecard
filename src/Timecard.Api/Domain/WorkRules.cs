namespace Timecard.Api.Domain;

/// <summary>
/// 1) 每天 9 小時（包含午休）
/// 2) 每天彈性時數最多累積/使用 55 分鐘
/// 3) 彈性時數月重置
/// </summary>
public static class WorkRules
{
    public const int PlannedMinutesPerWorkDay = 9 * 60;
    public const int DailyFlexCapMinutes = 55;

    public static DayComputed ComputeDay(int plannedMinutes, int workedMinutes, int creditedMinutes)
    {
        var effective = workedMinutes + creditedMinutes;
        var delta = effective - plannedMinutes;

        // 免上班日：不累積也不使用彈性（避免「放假還賺彈性」）
        var flexCandidate = plannedMinutes == 0 ? 0 : Math.Clamp(delta, -DailyFlexCapMinutes, DailyFlexCapMinutes);

        return new DayComputed(plannedMinutes, workedMinutes, creditedMinutes, effective, delta, flexCandidate);
    }

    public static MonthComputed ComputeMonth(IEnumerable<DayWithComputed> daysInAnyOrder)
    {
        var days = daysInAnyOrder.OrderBy(d => d.Date).ToList();
        var bank = 0;
        var results = new List<MonthDayComputed>(days.Count);

        foreach (var d in days)
        {
            if (d.Computed.PlannedMinutes == 0)
            {
                results.Add(new MonthDayComputed(d.Date, d.Computed, FlexApplied: 0, FlexBankEnd: bank, DeficitMinutes: 0));
                continue;
            }

            var desired = d.Computed.FlexCandidate; // [-55, +55]
            if (desired >= 0)
            {
                bank += desired;
                results.Add(new MonthDayComputed(d.Date, d.Computed, FlexApplied: desired, FlexBankEnd: bank, DeficitMinutes: 0));
                continue;
            }

            var need = -desired;
            var used = Math.Min(bank, need);
            bank -= used;

            var applied = -used;         // negative
            var deficit = need - used;   // 需要用請假/出差/補登等方式補

            results.Add(new MonthDayComputed(d.Date, d.Computed, FlexApplied: applied, FlexBankEnd: bank, DeficitMinutes: deficit));
        }

        return new MonthComputed(results, FlexBankEnd: bank);
    }
}

public sealed record DayComputed(
    int PlannedMinutes,
    int WorkedMinutes,
    int CreditedMinutes,
    int EffectiveMinutes,
    int DeltaMinutes,
    int FlexCandidate
);

public sealed record DayWithComputed(DateOnly Date, DayComputed Computed);

public sealed record MonthDayComputed(
    DateOnly Date,
    DayComputed Day,
    int FlexApplied,
    int FlexBankEnd,
    int DeficitMinutes
);

public sealed record MonthComputed(IReadOnlyList<MonthDayComputed> Days, int FlexBankEnd);
