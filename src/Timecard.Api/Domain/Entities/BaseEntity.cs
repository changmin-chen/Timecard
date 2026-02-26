namespace Timecard.Api.Domain.Entities;

public abstract class BaseEntity<TId> where TId : notnull, IEquatable<TId>
{
    public virtual TId Id { get; protected set; } = default!;
}
