namespace Timecard.Api.Data.Entities;

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

    public DomainResult<PunchEvent> AddPunch(DateTimeOffset at, string? note, TimeSpan minInterval, bool force)
    {
        var dateCheck = ValidatePunchDate(at);
        if (!dateCheck.IsSuccess) return DomainResult<PunchEvent>.Fail(dateCheck.Error!.Code, dateCheck.Error!.Message);

        var last = _punches.OrderByDescending(p => p.At).FirstOrDefault();
        if (!force && last is not null && (at - last.At) < minInterval)
            return DomainResult<PunchEvent>.Fail("too_fast", "Too fast. Please wait before creating another punch.");

        var punch = new PunchEvent(at, note);
        _punches.Add(punch);
        return DomainResult<PunchEvent>.Ok(punch);
    }

    public DomainResult RemovePunch(int punchId)
    {
        var punch = _punches.FirstOrDefault(p => p.Id == punchId);
        if (punch is null)
            return DomainResult.Fail("not_found", "Punch not found.");

        _punches.Remove(punch);
        return DomainResult.Ok();
    }

    public DomainResult<Adjustment> AddAdjustment(string kind, int minutes, string? note)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return DomainResult<Adjustment>.Fail("kind_required", "kind is required.");

        var adjustment = new Adjustment(kind, minutes, note);
        _adjustments.Add(adjustment);
        return DomainResult<Adjustment>.Ok(adjustment);
    }

    public DomainResult UpdateAdjustment(int adjustmentId, string kind, int minutes, string? note)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return DomainResult.Fail("kind_required", "kind is required.");

        var adjustment = _adjustments.FirstOrDefault(a => a.Id == adjustmentId);
        if (adjustment is null)
            return DomainResult.Fail("not_found", "Adjustment not found.");

        adjustment.Update(kind, minutes, note);
        return DomainResult.Ok();
    }

    public DomainResult RemoveAdjustment(int adjustmentId)
    {
        var adjustment = _adjustments.FirstOrDefault(a => a.Id == adjustmentId);
        if (adjustment is null)
            return DomainResult.Fail("not_found", "Adjustment not found.");

        _adjustments.Remove(adjustment);
        return DomainResult.Ok();
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

    private DomainResult ValidatePunchDate(DateTimeOffset at)
    {
        var punchDate = DateOnly.FromDateTime(at.LocalDateTime);
        if (punchDate != Date)
            return DomainResult.Fail("date_mismatch", "Changing punch date is not supported in MVP.");

        return DomainResult.Ok();
    }
}
