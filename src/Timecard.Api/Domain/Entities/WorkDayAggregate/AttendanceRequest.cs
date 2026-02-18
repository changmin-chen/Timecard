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
    public TimeRange Range { get; private set; }
    public string Note { get; private set; } = "";
    
    public TimeOnly Start => Range.Start;
    public TimeOnly End => Range.End;


    public AttendanceRequest(string category, TimeRange range, string? note)
    {
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.Default(range);
        
        Category = category.Trim();
        Range = range;
        Note = note?.Trim() ?? "";
    }
    

    public void Update(string category, TimeRange range, string? note)
    {
        Guard.Against.NullOrWhiteSpace(category);
        Guard.Against.Default(range);
        
        Category = category.Trim();
        Range = range;
        Note = note?.Trim() ?? "";
    }
}
