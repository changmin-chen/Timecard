using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Auth;

public static class AdminEndpoints
{
    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/admin").WithTags("Admin").RequireAuthorization("Admin");

        g.MapPost("/users", CreateUser);

        return app;
    }

    private record CreateUserRequest(string Email, string EmployeeId, string? DisplayName, string TemporaryPassword);

    // POST /api/admin/users
    private static async Task<IResult> CreateUser(
        CreateUserRequest body,
        TimecardDb db,
        IPasswordHasher<AppUser> hasher)
    {
        if (string.IsNullOrWhiteSpace(body.Email))
            return Results.BadRequest(new { message = "Email 不可為空。" });

        if (string.IsNullOrWhiteSpace(body.EmployeeId))
            return Results.BadRequest(new { message = "EmployeeId 不可為空。" });

        if (body.TemporaryPassword.Length < 8)
            return Results.BadRequest(new { message = "密碼至少需要 8 個字元。" });

        var exists = await db.Users.AnyAsync(u => u.Email.ToLower() == body.Email.ToLower());
        if (exists)
            return Results.Conflict(new { message = "此 Email 已被使用。" });

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
