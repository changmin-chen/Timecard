using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Results;
using Timecard.Api.Features.Auth;
using Timecard.Api.Features.Shared;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Admin;

public static class AdminEndpoints
{
    private const string BuiltInDefaultResetPassword = "ChangeMe123!";

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization(AuthRoles.Admin);

        g.MapGet("/users", ListUsers);
        g.MapPost("/users", CreateUser)
            .AddEndpointFilter<CreateUserValidationFilter>();
        g.MapPost("/users/{id}/reset-password", ResetPassword);

        return app;
    }

    private sealed record AdminUserDto(
        string Id,
        string Email,
        string EmployeeId,
        string? DisplayName,
        bool IsAdmin,
        bool MustChangePassword);

    internal record CreateUserRequest(string Email, string EmployeeId, string? DisplayName, string TemporaryPassword);

    // GET /api/admin/users
    private static async Task<IResult> ListUsers(TimecardDb db, CancellationToken ct)
    {
        var adminRoleId = await db.Roles
            .Where(r => r.Name == AuthRoles.Admin)
            .Select(r => r.Id)
            .FirstOrDefaultAsync(ct);

        var adminUserIds = new HashSet<string>();
        if (adminRoleId is not null)
        {
            adminUserIds = await db.UserRoles
                .Where(ur => ur.RoleId == adminRoleId)
                .Select(ur => ur.UserId)
                .ToHashSetAsync(ct);
        }

        var users = await db.Users
            .OrderBy(u => u.Email)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.EmployeeId,
                u.DisplayName,
                u.MustChangePassword,
            })
            .ToListAsync(ct);

        return Results.Ok(users.Select(u => new AdminUserDto(
            u.Id,
            u.Email ?? "",
            u.EmployeeId,
            u.DisplayName,
            adminUserIds.Contains(u.Id),
            u.MustChangePassword)));
    }

    // POST /api/admin/users
    private static async Task<IResult> CreateUser(
        CreateUserRequest body,
        HttpContext http,
        TimecardDb db,
        UserManager<AppUser> userManager,
        CancellationToken ct)
    {
        var normalizedEmail = userManager.NormalizeEmail(body.Email);
        var exists = await db.Users.AnyAsync(u => u.NormalizedEmail == normalizedEmail, ct);
        if (exists)
            return new Error("admin.email_taken", "This email is already taken.", ErrorKind.Conflict, "Conflict").ToProblem(http);

        var user = new AppUser(body.Email, body.EmployeeId, body.DisplayName)
        {
            MustChangePassword = true,
        };

        ct.ThrowIfCancellationRequested();
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

    // POST /api/admin/users/{id}/reset-password
    private static async Task<IResult> ResetPassword(
        string id,
        HttpContext http,
        IConfiguration config,
        TimecardDb db,
        UserManager<AppUser> userManager,
        CancellationToken ct)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct);
        if (user is null)
            return new Error("admin.user_not_found", "User not found.", ErrorKind.NotFound, "Not found").ToProblem(http);

        var defaultResetPassword = config["Admin:DefaultResetPassword"];
        if (string.IsNullOrWhiteSpace(defaultResetPassword))
            defaultResetPassword = BuiltInDefaultResetPassword;

        if (defaultResetPassword.Length < 8)
            return new Error(
                "admin.default_password_invalid",
                "Admin:DefaultResetPassword must be at least 8 characters.",
                ErrorKind.Unexpected,
                "Server configuration error").ToProblem(http);

        user.PasswordHash = userManager.PasswordHasher.HashPassword(user, defaultResetPassword);
        user.SecurityStamp = Guid.NewGuid().ToString("N");
        user.MustChangePassword = true;

        await db.SaveChangesAsync(ct);

        return Results.Ok(new
        {
            id = user.Id,
            email = user.Email,
            temporaryPassword = defaultResetPassword,
            mustChangePassword = user.MustChangePassword,
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
