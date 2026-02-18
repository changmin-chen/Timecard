namespace Timecard.Api.Domain.Entities;

public abstract class BaseEntity<TId> where TId : struct
{
    public virtual TId Id { get; protected set; }
}
