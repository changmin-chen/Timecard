using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.SyncPunch;

internal sealed class SyncPunchesValidationFilter : IEndpointFilter
{
    private const int MaxPunchesPerRequest = 5000;
    
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var req = ctx.GetArgument<SyncPunchesRequest>(0);

        if (req.Punches.Count == 0)
            return new Error("sync.empty_punches", "At least one punch entry is required.",
            ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);

        if (req.Punches.Count > MaxPunchesPerRequest)
            return new Error("sync.too_many_punches", $"Maximum {MaxPunchesPerRequest} punches are allowed per request.",
            ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);
            
        var badIndexes = req.Punches
            .Select((e, i) => (e, i))
            .Where(x => string.IsNullOrWhiteSpace(x.e.EmployeeId))
            .Select(x => x.i)
            .ToList();

        if (badIndexes.Count > 0)
            return new Error("sync.invalid_employee_id",
            $"EmployeeId is required for entries: {string.Join(", ", badIndexes)}.",
            ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);

        return await next(ctx);
    }
}
