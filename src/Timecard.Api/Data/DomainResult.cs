namespace Timecard.Api.Data;

public sealed record DomainError(string Code, string Message);

public readonly record struct DomainResult(DomainError? Error)
{
    public bool IsSuccess => Error is null;

    public static DomainResult Ok() => new(null);
    public static DomainResult Fail(string code, string message) => new(new DomainError(code, message));
}

public readonly record struct DomainResult<T>(T? Value, DomainError? Error)
{
    public bool IsSuccess => Error is null;

    public static DomainResult<T> Ok(T value) => new(value, null);
    public static DomainResult<T> Fail(string code, string message) => new(default, new DomainError(code, message));
}

public static class DomainResultExtensions
{
    public static IResult? ToErrorResult(this DomainResult result) =>
        result.IsSuccess ? null
        : result.Error!.Code == "not_found" ? Results.NotFound()
        : Results.BadRequest(new { error = result.Error!.Message });

    public static IResult? ToErrorResult<T>(this DomainResult<T> result) =>
        result.IsSuccess ? null
        : result.Error!.Code == "not_found" ? Results.NotFound()
        : Results.BadRequest(new { error = result.Error!.Message });
}
