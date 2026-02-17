using Timecard.Api.Domain.Results;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Domain.Entities;

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

    public IReadOnlyList<PunchEvent> Punches => _punches;
    public IReadOnlyList<AttendanceRequest> AttendanceRequests => _attendanceRequests;

    public Result<PunchEvent> AddPunch(DateTimeOffset at, string? note, TimeSpan minInterval, bool force)
    {
        var dateCheck = ValidatePunchDate(at);
        if (!dateCheck.IsSuccess) return Result<PunchEvent>.Fail(dateCheck.Error!);

        var last = _punches.OrderByDescending(p => p.At).FirstOrDefault();
        if (!force && last is not null && (at - last.At) < minInterval)
            return Result<PunchEvent>.Fail(Errors.WorkDay.PunchTooFast);

        var punch = new PunchEvent(at, note);
        _punches.Add(punch);
        return Result<PunchEvent>.Ok(punch);
    }

    public Result RemovePunch(int punchId)
    {
        var punch = _punches.FirstOrDefault(p => p.Id == punchId);
        if (punch is null)
            return Result.Fail(Errors.WorkDay.PunchNotFound);

        _punches.Remove(punch);
        return Result.Ok();
    }

    public Result<AttendanceRequest> AddAttendanceRequest(string category, TimeOnly start, TimeOnly end, string? note)
    {
        var createResult = AttendanceRequest.Create(category, start, end, note);
        if (!createResult.IsSuccess) return createResult;

        var overlapCheck = ValidateNoOverlap(start, end, excludeId: null);
        if (!overlapCheck.IsSuccess) return Result<AttendanceRequest>.Fail(overlapCheck.Error!);

        var gapCheck = ValidateRequestAgainstPunchSpan(start, end, excludeId: null);
        if (!gapCheck.IsSuccess) return Result<AttendanceRequest>.Fail(gapCheck.Error!);

        _attendanceRequests.Add(createResult.Value!);
        return createResult;
    }

    public Result UpdateAttendanceRequest(int id, string category, TimeOnly start, TimeOnly end, string? note)
    {
        var request = _attendanceRequests.FirstOrDefault(a => a.Id == id);
        if (request is null)
            return Result.Fail(Errors.WorkDay.AttendanceNotFound);

        var overlapCheck = ValidateNoOverlap(start, end, excludeId: id);
        if (!overlapCheck.IsSuccess) return overlapCheck;

        var gapCheck = ValidateRequestAgainstPunchSpan(start, end, excludeId: id);
        if (!gapCheck.IsSuccess) return gapCheck;

        return request.Update(category, start, end, note);
    }

    public Result RemoveAttendanceRequest(int id)
    {
        var request = _attendanceRequests.FirstOrDefault(a => a.Id == id);
        if (request is null)
            return Result.Fail(Errors.WorkDay.AttendanceNotFound);

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

    private Result ValidateNoOverlap(TimeOnly start, TimeOnly end, int? excludeId)
    {
        foreach (var existing in _attendanceRequests)
        {
            if (excludeId.HasValue && existing.Id == excludeId.Value) continue;

            if (start < existing.End && end > existing.Start)
                return Result.Fail(Errors.WorkDay.Overlap);
        }

        return Result.Ok();
    }

    private Result ValidateRequestAgainstPunchSpan(TimeOnly newStart, TimeOnly newEnd, int? excludeId)
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
            return Result.Ok();

        // Sort by start time and check for gaps
        segments.Sort((a, b) => a.Start.CompareTo(b.Start));

        for (int i = 1; i < segments.Count; i++)
        {
            if (segments[i].Start > segments[i - 1].End)
                return Result.Fail(Errors.WorkDay.Gap);
        }

        return Result.Ok();
    }

    private Result ValidatePunchDate(DateTimeOffset at)
    {
        var punchDate = DateOnly.FromDateTime(at.LocalDateTime);
        if (punchDate != Date)
            return Result.Fail(Errors.WorkDay.InvalidPunchDate);

        return Result.Ok();
    }
}
