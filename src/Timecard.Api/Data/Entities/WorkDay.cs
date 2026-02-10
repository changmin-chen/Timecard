namespace Timecard.Api.Data.Entities;

public sealed class WorkDay
{
    private readonly List<PunchEvent> _punches = [];
    private readonly List<AttendanceRequest> _attendanceRequests = [];

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
    public IReadOnlyList<AttendanceRequest> AttendanceRequests => _attendanceRequests;

    public void SetNonWorking(bool isNonWorkingDay, string? note)
    {
        IsNonWorkingDay = isNonWorkingDay;
        Note = note?.Trim() ?? "";
    }

    public DomainResult<PunchEvent> AddPunch(DateTimeOffset at, string? note, TimeSpan minInterval, bool force)
    {
        var dateCheck = ValidatePunchDate(at);
        if (!dateCheck.IsSuccess) return dateCheck.Error!.Message;

        var last = _punches.OrderByDescending(p => p.At).FirstOrDefault();
        if (!force && last is not null && (at - last.At) < minInterval)
            return "Too fast. Please wait before creating another punch.";

        var punch = new PunchEvent(at, note);
        _punches.Add(punch);
        return DomainResult<PunchEvent>.Ok(punch);
    }

    public DomainResult RemovePunch(int punchId)
    {
        var punch = _punches.FirstOrDefault(p => p.Id == punchId);
        if (punch is null)
            return "Punch not found.";

        _punches.Remove(punch);
        return DomainResult.Ok();
    }

    public DomainResult<AttendanceRequest> AddAttendanceRequest(string category, TimeOnly start, TimeOnly end, string? note)
    {
        if (string.IsNullOrWhiteSpace(category))
            return "Category is required.";

        if (start >= end)
            return "Start must be before End.";

        var overlapCheck = CheckOverlap(start, end, excludeId: null);
        if (!overlapCheck.IsSuccess) return overlapCheck.Error!.Message;

        var gapCheck = CheckGaps(start, end, excludeId: null);
        if (!gapCheck.IsSuccess) return gapCheck.Error!.Message;

        var request = new AttendanceRequest(category, start, end, note);
        _attendanceRequests.Add(request);
        return DomainResult<AttendanceRequest>.Ok(request);
    }

    public DomainResult UpdateAttendanceRequest(int id, string category, TimeOnly start, TimeOnly end, string? note)
    {
        if (string.IsNullOrWhiteSpace(category))
            return "Category is required.";

        if (start >= end)
            return "Start must be before End.";

        var request = _attendanceRequests.FirstOrDefault(a => a.Id == id);
        if (request is null)
            return "Attendance request not found.";

        var overlapCheck = CheckOverlap(start, end, excludeId: id);
        if (!overlapCheck.IsSuccess) return overlapCheck.Error!.Message;

        var gapCheck = CheckGaps(start, end, excludeId: id);
        if (!gapCheck.IsSuccess) return gapCheck.Error!.Message;

        request.Update(category, start, end, note);
        return DomainResult.Ok();
    }

    public DomainResult RemoveAttendanceRequest(int id)
    {
        var request = _attendanceRequests.FirstOrDefault(a => a.Id == id);
        if (request is null)
            return "Attendance request not found.";

        _attendanceRequests.Remove(request);
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

    public int CalculateExtensionMinutes()
    {
        var ordered = _punches.OrderBy(p => p.At).ToList();
        var hasPunchSpan = ordered.Count >= 2;
        var hasRequests = _attendanceRequests.Count > 0;

        if (!hasPunchSpan && !hasRequests) return 0;

        TimeOnly? punchStart = null;
        TimeOnly? punchEnd = null;

        if (hasPunchSpan)
        {
            punchStart = TimeOnly.FromDateTime(ordered[0].At.LocalDateTime);
            punchEnd = TimeOnly.FromDateTime(ordered[^1].At.LocalDateTime);
        }

        var allStarts = _attendanceRequests.Select(r => r.Start).ToList();
        var allEnds = _attendanceRequests.Select(r => r.End).ToList();

        if (punchStart.HasValue) allStarts.Add(punchStart.Value);
        if (punchEnd.HasValue) allEnds.Add(punchEnd.Value);

        var effectiveStart = allStarts.Min();
        var effectiveEnd = allEnds.Max();

        var totalSpan = (effectiveEnd - effectiveStart).TotalMinutes;

        if (hasPunchSpan)
        {
            var punchSpan = (punchEnd!.Value - punchStart!.Value).TotalMinutes;
            return (int)(totalSpan - punchSpan);
        }

        // No punch span: entire effective range is extension
        return (int)totalSpan;
    }

    private DomainResult CheckOverlap(TimeOnly start, TimeOnly end, int? excludeId)
    {
        foreach (var existing in _attendanceRequests)
        {
            if (excludeId.HasValue && existing.Id == excludeId.Value) continue;

            if (start < existing.End && end > existing.Start)
                return "Attendance request overlaps with an existing one.";
        }

        return DomainResult.Ok();
    }

    private DomainResult CheckGaps(TimeOnly newStart, TimeOnly newEnd, int? excludeId)
    {
        var segments = new List<(TimeOnly Start, TimeOnly End)>();

        // Add punch span if available
        var ordered = _punches.OrderBy(p => p.At).ToList();
        if (ordered.Count >= 2)
        {
            var ps = TimeOnly.FromDateTime(ordered[0].At.LocalDateTime);
            var pe = TimeOnly.FromDateTime(ordered[^1].At.LocalDateTime);
            segments.Add((ps, pe));
        }

        // Add existing attendance requests (excluding the one being updated)
        foreach (var r in _attendanceRequests)
        {
            if (excludeId.HasValue && r.Id == excludeId.Value) continue;
            segments.Add((r.Start, r.End));
        }

        // Add the new/updated request
        segments.Add((newStart, newEnd));

        // If there's only one segment, no gaps possible
        if (segments.Count <= 1)
            return DomainResult.Ok();

        // Sort by start time and check for gaps
        segments.Sort((a, b) => a.Start.CompareTo(b.Start));

        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i].Start > segments[i - 1].End)
                return "There is a gap between time segments. Attendance requests must be contiguous with punch time.";
        }

        return DomainResult.Ok();
    }

    private DomainResult ValidatePunchDate(DateTimeOffset at)
    {
        var punchDate = DateOnly.FromDateTime(at.LocalDateTime);
        if (punchDate != Date)
            return "Changing punch date is not supported in MVP.";

        return DomainResult.Ok();
    }
}
