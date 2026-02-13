using System.Diagnostics;
using Timecard.Api.Domain.Result;

namespace Timecard.Api.Features.Shared;

public static class ResultHttpExtensions
{
    public static IResult ToIResult(this Result result, HttpContext http, Func<IResult> onSuccess)
        => result.IsSuccess ? onSuccess() : result.Error!.ToProblem(http);

    public static IResult ToIResult<T>(this Result<T> result, HttpContext http, Func<T, IResult> onSuccess)
        => result.IsSuccess ? onSuccess(result.Value!) : result.Error!.ToProblem(http);

    public static IResult ToProblem(this Error error, HttpContext http)
    {
        var status = error.Kind switch
        {
            ErrorKind.Validation => StatusCodes.Status400BadRequest,
            ErrorKind.NotFound => StatusCodes.Status404NotFound,
            ErrorKind.Conflict => StatusCodes.Status409Conflict,
            ErrorKind.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        return Results.Problem(
            title: error.Title ?? "Request failed",
            detail: error.Message,
            statusCode: status,
            instance: http.Request.Path,
            extensions: new Dictionary<string, object?>
            {
                ["code"] = error.Code,
                ["traceId"] = Activity.Current?.Id ?? http.TraceIdentifier
            }
        );
    }
}
