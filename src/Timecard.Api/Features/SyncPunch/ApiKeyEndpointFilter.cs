namespace Timecard.Api.Features.SyncPunch;

public class ApiKeyEndpointFilter(IConfiguration config) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var expectedKey = config["SyncPunch:ApiKey"];
        if (string.IsNullOrEmpty(expectedKey))
            return Results.Problem("SyncPunch endpoint is not configured.", statusCode: StatusCodes.Status503ServiceUnavailable);

        if (!ctx.HttpContext.Request.Headers.TryGetValue("X-Api-Key", out var providedKey)
            || providedKey != expectedKey)
            return Results.Unauthorized();

        return await next(ctx);
    }
}
