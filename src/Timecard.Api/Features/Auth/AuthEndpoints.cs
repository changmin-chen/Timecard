using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Infrastructure.Data;

namespace Timecard.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth");

        g.MapPost("/login", Login).AllowAnonymous();
        g.MapPost("/logout", (Delegate)Logout).AllowAnonymous();
        g.MapGet("/me", Me).RequireAuthorization();
        g.MapPost("/change-password", ChangePassword).RequireAuthorization();

        return app;
    }

    private record LoginRequest(string Email, string Password);
    private record ChangePasswordRequest(string CurrentPassword, string NewPassword);

    // POST /api/auth/login
    private static async Task<IResult> Login(
        LoginRequest body,
        TimecardDb db,
        IPasswordHasher<AppUser> hasher,
        HttpContext http)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == body.Email.ToLower());

        if (user is null || user.PasswordHash is null)
            return Results.Unauthorized();

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, body.Password);
        if (result == PasswordVerificationResult.Failed)
            return Results.Unauthorized();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.DisplayName),
        };
        if (user.IsAdmin)
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        return Results.Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.DisplayName,
            isAdmin = user.IsAdmin,
            mustChangePassword = user.MustChangePassword,
        });
    }

    // POST /api/auth/logout
    private static async Task<IResult> Logout(HttpContext http)
    {
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }

    // GET /api/auth/me — returns current user info from DB
    private static async Task<IResult> Me(ClaimsPrincipal principal, TimecardDb db)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Results.Unauthorized();

        var user = await db.Users.FindAsync(userId);
        if (user is null) return Results.Unauthorized();

        return Results.Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.DisplayName,
            isAdmin = user.IsAdmin,
            mustChangePassword = user.MustChangePassword,
        });
    }

    // POST /api/auth/change-password
    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest body,
        ClaimsPrincipal principal,
        TimecardDb db,
        IPasswordHasher<AppUser> hasher,
        HttpContext http)
    {
        var userId = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId is null) return Results.Unauthorized();

        var user = await db.Users.FindAsync(userId);
        if (user is null || user.PasswordHash is null) return Results.Unauthorized();

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, body.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            return Results.BadRequest(new { message = "目前密碼不正確。" });

        if (body.NewPassword.Length < 8)
            return Results.BadRequest(new { message = "新密碼至少需要 8 個字元。" });

        user.PasswordHash = hasher.HashPassword(user, body.NewPassword);
        user.MustChangePassword = false;
        await db.SaveChangesAsync();

        // 重新登入刷新 claim
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.NoContent();
    }
}
