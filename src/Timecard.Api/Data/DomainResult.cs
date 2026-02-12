namespace Timecard.Api.Data;

public sealed record DomainError(string Message);

public readonly record struct DomainResult(DomainError? Error)
{
    public bool IsSuccess => Error is null;

    public static DomainResult Ok() => new(null);
    public static DomainResult Fail(string message) => new(new DomainError(message));

    public static implicit operator DomainResult(string message) => Fail(message);
}

public readonly record struct DomainResult<T>(T? Value, DomainError? Error)
{
    public bool IsSuccess => Error is null;

    public static DomainResult<T> Ok(T value) => new(value, null);
    public static DomainResult<T> Fail(string message) => new(default, new DomainError(message));

    public static implicit operator DomainResult<T>(string message) => Fail(message);
}

public static class DomainResultExtensions
{
    public static IResult? ToErrorResult(this DomainResult result) =>
        result.IsSuccess ? null
        : Results.BadRequest(new { error = result.Error!.Message });

    public static IResult? ToErrorResult<T>(this DomainResult<T> result) =>
        result.IsSuccess ? null
        : Results.BadRequest(new { error = result.Error!.Message });
}
