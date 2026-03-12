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
            return Errors.Admin.EmailTaken.ToProblem(http);

        var user = new AppUser(body.Email, body.EmployeeId, body.DisplayName)
        {
            MustChangePassword = true,
        };

        ct.ThrowIfCancellationRequested();
        var result = await userManager.CreateAsync(user, body.TemporaryPassword);
        if (!result.Succeeded)
        {
            var message = string.Join(" ", result.Errors.Select(e => e.Description));
            return Errors.Admin.CreateUserFailed(message).ToProblem(http);
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
            return Errors.Admin.UserNotFound.ToProblem(http);

        var defaultResetPassword = config["Admin:DefaultResetPassword"];
        if (string.IsNullOrWhiteSpace(defaultResetPassword))
            defaultResetPassword = BuiltInDefaultResetPassword;

        if (defaultResetPassword.Length < 8)
            return Errors.Admin.DefaultPasswordInvalid.ToProblem(http);

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
            return Errors.Admin.InvalidEmail.ToProblem(ctx.HttpContext);

        if (string.IsNullOrWhiteSpace(req.EmployeeId))
            return Errors.Admin.InvalidEmployeeId.ToProblem(ctx.HttpContext);

        if (string.IsNullOrEmpty(req.TemporaryPassword) || req.TemporaryPassword.Length < 8)
            return Errors.Admin.PasswordTooShort.ToProblem(ctx.HttpContext);

        return await next(ctx);
    }
}
