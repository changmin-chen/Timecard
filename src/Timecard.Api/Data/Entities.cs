namespace Timecard.Api.Data;

public sealed class WorkDay
{
    private readonly List<PunchEvent> _punches = [];
    private readonly List<Adjustment> _adjustments = [];

    private WorkDay()
    {
    }

    public WorkDay(DateOnly date)
    {
        Date = date;
    }

    public int Id { get; private set; }
    public DateOnly Date { get; private set; }

    public bool IsNonWorkingDay { get; private set; }
    public string Note { get; private set; } = "";

    public IReadOnlyList<PunchEvent> Punches => _punches;
    public IReadOnlyList<Adjustment> Adjustments => _adjustments;

    public void SetNonWorking(bool isNonWorkingDay, string? note)
    {
        IsNonWorkingDay = isNonWorkingDay;
        Note = note?.Trim() ?? "";
    }

    public PunchEvent AddPunch(DateTimeOffset at, string? note, TimeSpan minInterval, bool force)
    {
        EnsurePunchDateMatchesWorkDay(at);

        var last = _punches.OrderByDescending(p => p.At).FirstOrDefault();
        if (!force && last is not null && (at - last.At) < minInterval)
            throw new InvalidOperationException("Too fast. Please wait before creating another punch.");

        var punch = new PunchEvent(at, note);
        _punches.Add(punch);
        return punch;
    }

    public void UpdatePunch(int punchId, DateTimeOffset at, string? note)
    {
        EnsurePunchDateMatchesWorkDay(at);

        var punch = _punches.FirstOrDefault(p => p.Id == punchId);
        if (punch is null)
            throw new KeyNotFoundException("Punch not found.");

        punch.Update(at, note);
    }

    public void RemovePunch(int punchId)
    {
        var punch = _punches.FirstOrDefault(p => p.Id == punchId);
        if (punch is null)
            throw new KeyNotFoundException("Punch not found.");

        _punches.Remove(punch);
    }

    public Adjustment AddAdjustment(string kind, int minutes, string? note)
    {
        if (string.IsNullOrWhiteSpace(kind))
            throw new InvalidOperationException("kind is required.");

        var adjustment = new Adjustment(kind, minutes, note);
        _adjustments.Add(adjustment);
        return adjustment;
    }

    public void UpdateAdjustment(int adjustmentId, string kind, int minutes, string? note)
    {
        if (string.IsNullOrWhiteSpace(kind))
            throw new InvalidOperationException("kind is required.");

        var adjustment = _adjustments.FirstOrDefault(a => a.Id == adjustmentId);
        if (adjustment is null)
            throw new KeyNotFoundException("Adjustment not found.");

        adjustment.Update(kind, minutes, note);
    }

    public void RemoveAdjustment(int adjustmentId)
    {
        var adjustment = _adjustments.FirstOrDefault(a => a.Id == adjustmentId);
        if (adjustment is null)
            throw new KeyNotFoundException("Adjustment not found.");

        _adjustments.Remove(adjustment);
    }

    public (DateTimeOffset? Start, DateTimeOffset? End, int WorkedMinutes) DeriveSpan()
    {
        var ordered = _punches.OrderBy(p => p.At).ToList();
        if (ordered.Count == 0) return (null, null, 0);

        var start = ordered[0].At;
        if (ordered.Count == 1) return (start, null, 0);

        var end = ordered[^1].At;
        var workedMinutes = (int)Math.Max(0, (end - start).TotalMinutes);
        return (start, end, workedMinutes);
    }

    public int CreditedMinutes => _adjustments.Sum(a => a.Minutes);

    private void EnsurePunchDateMatchesWorkDay(DateTimeOffset at)
    {
        var punchDate = DateOnly.FromDateTime(at.LocalDateTime);
        if (punchDate != Date)
            throw new InvalidOperationException("Changing punch date is not supported in MVP.");
    }
}

public sealed class PunchEvent
{
    private PunchEvent()
    {
    }

    internal PunchEvent(DateTimeOffset at, string? note)
    {
        At = at;
        Note = note?.Trim() ?? "";
    }

    public int Id { get; private set; }
    public int WorkDayId { get; private set; }

    public DateTimeOffset At { get; private set; }
    public string Note { get; private set; } = "";

    internal void Update(DateTimeOffset at, string? note)
    {
        At = at;
        Note = note?.Trim() ?? "";
    }
}

public sealed class Adjustment
{
    private Adjustment()
    {
    }

    internal Adjustment(string kind, int minutes, string? note)
    {
        Kind = kind.Trim();
        Minutes = minutes;
        Note = note?.Trim() ?? "";
    }

    public int Id { get; private set; }
    public int WorkDayId { get; private set; }

    public string Kind { get; private set; } = "Manual";
    public int Minutes { get; private set; }
    public string Note { get; private set; } = "";

    internal void Update(string kind, int minutes, string? note)
    {
        Kind = kind.Trim();
        Minutes = minutes;
        Note = note?.Trim() ?? "";
    }
}
