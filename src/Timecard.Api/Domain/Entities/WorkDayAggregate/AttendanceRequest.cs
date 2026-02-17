using Ardalis.GuardClauses;
using Timecard.Api.Domain.Results;

namespace Timecard.Api.Domain.Entities.WorkDayAggregate;

public sealed class AttendanceRequest : BaseEntity<int>
{
    private AttendanceRequest()
    {
    }

    public int WorkDayId { get; private set; }

    public string Category { get; private set; } = "Leave";
    public TimeOnly Start { get; private set; }
    public TimeOnly End { get; private set; }
    public string Note { get; private set; } = "";

    public TimeRange Range => new(Start, End);

    public AttendanceRequest(string category, TimeRange range, string? note)
    {
        Guard.Against.NullOrWhiteSpace(category);
        
        Category = category.Trim();
        Start = range.Start;
        End = range.End;
        Note = note?.Trim() ?? "";
    }
    

    public void Update(string category, TimeRange range, string? note)
    {
        Guard.Against.NullOrWhiteSpace(category);
        
        Category = category.Trim();
        Start = range.Start;
        End = range.End;
        Note = note?.Trim() ?? "";
    }
}
