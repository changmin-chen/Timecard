using Timecard.Api.Domain.Results;

namespace Timecard.Api.Domain.Entities.WorkDayAggregate;

public sealed class WorkDay : BaseEntity<int>
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

    public DateOnly Date { get; private set; }

    public IReadOnlyCollection<PunchEvent> Punches => _punches.AsReadOnly();
    public IReadOnlyCollection<AttendanceRequest> AttendanceRequests => _attendanceRequests.AsReadOnly();

    public Result<PunchEvent> AddPunch(DateTimeOffset at, string? note, TimeSpan minInterval, bool force)
    {
        var dateCheck = ValidatePunchDate(at);
        if (!dateCheck.IsSuccess) return dateCheck.Error!;

        var last = _punches.OrderByDescending(p => p.At).FirstOrDefault();
        if (!force && last is not null && (at - last.At) < minInterval)
            return Errors.WorkDay.PunchTooFast;

        var punch = new PunchEvent(at, note);
        _punches.Add(punch);
        return punch;
    }

    public Result RemovePunch(int punchId)
    {
        var punch = _punches.FirstOrDefault(p => p.Id == punchId);
        if (punch is null)
            return Errors.WorkDay.PunchNotFound;

        _punches.Remove(punch);
        return Result.Ok();
    }


    public Result<AttendanceRequest> AddAttendanceRequest(string category, TimeRange range, string? note)
    {
        var overlapCheck = ValidateNoOverlap(range, excludeId: null);
        if (!overlapCheck.IsSuccess) return overlapCheck.Error!;

        var gapCheck = ValidateRequestAgainstPunchSpan(range, excludeId: null);
        if (!gapCheck.IsSuccess) return gapCheck.Error!;

        var newAttendance = new AttendanceRequest(category, range, note);
        _attendanceRequests.Add(newAttendance);
        return newAttendance;
    }

    public Result UpdateAttendanceRequest(int id, string category, TimeRange range, string? note)
    {
        var request = _attendanceRequests.FirstOrDefault(a => a.Id == id);
        if (request is null)
            return Errors.WorkDay.AttendanceNotFound;

        var overlapCheck = ValidateNoOverlap(range, excludeId: id);
        if (!overlapCheck.IsSuccess) return overlapCheck;

        var gapCheck = ValidateRequestAgainstPunchSpan(range, excludeId: id);
        if (!gapCheck.IsSuccess) return gapCheck;

        request.Update(category, range, note);
        return Result.Ok();
    }

    public Result RemoveAttendanceRequest(int id)
    {
        var request = _attendanceRequests.FirstOrDefault(a => a.Id == id);
        if (request is null)
            return Errors.WorkDay.AttendanceNotFound;

        _attendanceRequests.Remove(request);
        return Result.Ok();
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
        var punchSpan = GetPunchSpan();
        if (punchSpan is null && _attendanceRequests.Count == 0) return 0;

        var allRanges = _attendanceRequests.Select(r => r.Range).ToList();
        if (punchSpan is not null) allRanges.Add(punchSpan.Value);

        var effectiveStart = allRanges.Min(r => r.Start);
        var effectiveEnd = allRanges.Max(r => r.End);
        var totalSpan = (effectiveEnd - effectiveStart).TotalMinutes;

        return punchSpan is not null
            ? (int)(totalSpan - punchSpan.Value.Duration.TotalMinutes)
            : (int)totalSpan;
    }

    private Result ValidateNoOverlap(TimeRange range, int? excludeId)
    {
        foreach (var existing in _attendanceRequests)
        {
            if (excludeId.HasValue && existing.Id == excludeId.Value) continue;

            if (range.Overlaps(existing.Range))
                return Errors.WorkDay.Overlap;
        }

        return Result.Ok();
    }

    private Result ValidateRequestAgainstPunchSpan(TimeRange range, int? excludeId)
    {
        var segments = new List<TimeRange> { range };

        if (GetPunchSpan() is { } punchSpan)
            segments.Add(punchSpan);

        foreach (var r in _attendanceRequests)
        {
            if (excludeId.HasValue && r.Id == excludeId.Value) continue;
            segments.Add(r.Range);
        }

        return TimeRange.HasGaps(segments)
            ? Errors.WorkDay.HasGap
            : Result.Ok();
    }

    private TimeRange? GetPunchSpan()
    {
        var ordered = _punches.OrderBy(p => p.At).ToList();
        if (ordered.Count < 2) return null;

        var start = TimeOnly.FromDateTime(ordered[0].At.LocalDateTime);
        var end = TimeOnly.FromDateTime(ordered[^1].At.LocalDateTime);
        return new TimeRange(start, end);
    }

    private Result ValidatePunchDate(DateTimeOffset at)
    {
        var punchDate = DateOnly.FromDateTime(at.LocalDateTime);
        if (punchDate != Date)
            return Errors.WorkDay.InvalidPunchDate;

        return Result.Ok();
    }
}
