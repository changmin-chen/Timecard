using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Shared;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Auth;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization("Admin");

        g.MapPost("/users", CreateUser)
            .AddEndpointFilter<CreateUserValidationFilter>();

        return app;
    }

    internal record CreateUserRequest(string Email, string EmployeeId, string? DisplayName, string TemporaryPassword);

    // POST /api/admin/users
    private static async Task<IResult> CreateUser(
        CreateUserRequest body,
        HttpContext http,
        TimecardDb db,
        IPasswordHasher<AppUser> hasher)
    {
        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == body.Email.ToLower());
        if (exists)
            return new Error("admin.email_taken", "This email is already taken.", ErrorKind.Conflict, "Conflict").ToProblem(http);

        var user = new AppUser(Guid.NewGuid().ToString(), body.Email, body.EmployeeId, body.DisplayName)
        {
            MustChangePassword = true,
            IsAdmin = false,
        };
        user.PasswordHash = hasher.HashPassword(user, body.TemporaryPassword);

        db.Users.Add(user);
        await db.SaveChangesAsync();

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
