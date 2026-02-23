using Microsoft.AspNetCore.Identity;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Shared;

namespace Timecard.Api.Features.Auth;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization(AuthRoles.Admin);

        g.MapPost("/users", CreateUser)
            .AddEndpointFilter<CreateUserValidationFilter>();

        return app;
    }

    internal record CreateUserRequest(string Email, string EmployeeId, string? DisplayName, string TemporaryPassword);

    // POST /api/admin/users
    private static async Task<IResult> CreateUser(
        CreateUserRequest body,
        HttpContext http,
        UserManager<AppUser> userManager)
    {
        var exists = await userManager.FindByEmailAsync(body.Email);
        if (exists is not null)
            return new Error("admin.email_taken", "This email is already taken.", ErrorKind.Conflict, "Conflict").ToProblem(http);

        var user = new AppUser(body.Email, body.EmployeeId, body.DisplayName)
        {
            MustChangePassword = true,
        };

        var result = await userManager.CreateAsync(user, body.TemporaryPassword);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.Description));
            return new Error("admin.create_user_failed", message, ErrorKind.Validation, "Invalid request").ToProblem(http);
        }

        return Results.Created($"/api/admin/users/{user.Id}", new
        {
            id = user.Id,
            email = user.Email,
            employeeId = user.EmployeeId,
            displayName = user.DisplayName,
        });
    }
}

internal sealed class CreateUserValidationFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext ctx, EndpointFilterDelegate next)
    {
        var req = ctx.GetArgument<AdminEndpoints.CreateUserRequest>(0);

        if (string.IsNullOrWhiteSpace(req.Email))
            return new Error("admin.invalid_email", "Email is required.",
            ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);

        if (string.IsNullOrWhiteSpace(req.EmployeeId))
            return new Error("admin.invalid_employee_id", "EmployeeId is required.",
            ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);

        if (string.IsNullOrEmpty(req.TemporaryPassword) || req.TemporaryPassword.Length < 8)
            return new Error("admin.password_too_short", "Password must be at least 8 characters.",
            ErrorKind.Validation, "Invalid request").ToProblem(ctx.HttpContext);

        return await next(ctx);
    }
}
