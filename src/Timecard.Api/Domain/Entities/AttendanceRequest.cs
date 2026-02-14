using Timecard.Api.Domain.Results;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Domain.Entities;

public sealed class AttendanceRequest
{
    private AttendanceRequest()
    {
    }

    public int Id { get; private set; }
    public int WorkDayId { get; private set; }

    public string Category { get; private set; } = "Leave";
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }
    public string Note { get; private set; } = "";

    internal static Result<AttendanceRequest> Create(string category, TimeOnly start, TimeOnly end, string? note)
    {
        var validation = Validate(category, start, end);
        if (!validation.IsSuccess) return Result<AttendanceRequest>.Fail(validation.Error!);

        var request = new AttendanceRequest
        {
            Category = category.Trim(),
            Start = start,
            End = end,
            Note = note?.Trim() ?? ""
        };
        return Result<AttendanceRequest>.Ok(request);
    }

    internal Result Update(string category, TimeOnly start, TimeOnly end, string? note)
    {
        var validation = Validate(category, start, end);
        if (!validation.IsSuccess) return validation;

        Category = category.Trim();
        Start = start;
        End = end;
        Note = note?.Trim() ?? "";
        return Result.Ok();
    }

    private static Result Validate(string category, TimeOnly start, TimeOnly end)
    {
        if (string.IsNullOrWhiteSpace(category))
            return Result.Fail(Errors.WorkDay.CategoryRequired);

        if (start >= end)
            return Result.Fail(Errors.WorkDay.StartBeforeEnd);

        return Result.Ok();
    }
}
