namespace Timecard.Api.Domain.Results;

public enum ErrorKind
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Forbidden = 4,
    Unauthorized = 5,
    Unexpected = 99
}

public sealed record Error(string Code, string Message, ErrorKind Kind, string? Title = null)
{
    public static readonly Error None = new("", "", ErrorKind.None);
    public bool IsNone => Kind == ErrorKind.None;
}

public readonly record struct Result(Error? Error)
{
    public bool IsSuccess => Error is null;

    public static Result Ok() => new(null);

    public static Result Fail(Error error)
    {
        if (error.IsNone) throw new ArgumentException("Failure must carry a real error.", nameof(error));
        return new Result(error);
    }
    
    public static implicit operator Result(Error error) => Fail(error);
}

public readonly record struct Result<T>(T? Value, Error? Error)
{
    public bool IsSuccess => Error is null;

    public static Result<T> Ok(T value) => new(value, null);

    public static Result<T> Fail(Error error)
    {
        if (error.IsNone) throw new ArgumentException("Failure must carry a real error.", nameof(error));
        return new Result<T>(default, error);
    }
    
    public static implicit operator Result<T>(T value) => Ok(value);
    public static implicit operator Result<T>(Error error) => Fail(error);
}
