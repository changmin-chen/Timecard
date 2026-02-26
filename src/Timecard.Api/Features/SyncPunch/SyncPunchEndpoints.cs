namespace Timecard.Api.Features.SyncPunch;

public static class SyncPunchEndpoints
{
    public static IEndpointRouteBuilder MapSyncPunchEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/sync-punch").WithTags("SyncPunch").AllowAnonymous();
        
        g.MapPost("/punches", (SyncPunchesRequest req, SyncPunchHandler handler, HttpContext http, CancellationToken ct) =>
                handler.Handle(req, http, ct))
            .AddEndpointFilter<ApiKeyEndpointFilter>()
            .AddEndpointFilter<SyncPunchesValidationFilter>();

        return app;
    }
}
