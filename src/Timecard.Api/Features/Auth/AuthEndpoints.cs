using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Timecard.Api.Domain.Entities;

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
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        var user = await userManager.FindByEmailAsync(body.Email);
        if (user is null)
            return Results.Unauthorized();

        var check = await signInManager.CheckPasswordSignInAsync(user, body.Password, lockoutOnFailure: false);
        if (!check.Succeeded)
            return Results.Unauthorized();

        await signInManager.SignInAsync(user, isPersistent: false);
        var isAdmin = await userManager.IsInRoleAsync(user, AuthRoles.Admin);

        return Results.Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.DisplayName ?? user.Email,
            isAdmin,
            mustChangePassword = user.MustChangePassword,
        });
    }

    // POST /api/auth/logout
    private static async Task<IResult> Logout(SignInManager<AppUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.Ok();
    }

    // GET /api/auth/me returns current user info from DB
    private static async Task<IResult> Me(ClaimsPrincipal principal, UserManager<AppUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var isAdmin = await userManager.IsInRoleAsync(user, AuthRoles.Admin);

        return Results.Ok(new
        {
            id = user.Id,
            email = user.Email,
            name = user.DisplayName ?? user.Email,
            isAdmin,
            mustChangePassword = user.MustChangePassword,
        });
    }

    // POST /api/auth/change-password
    private static async Task<IResult> ChangePassword(
        ChangePasswordRequest body,
        ClaimsPrincipal principal,
        UserManager<AppUser> userManager,
        SignInManager<AppUser> signInManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user is null)
            return Results.Unauthorized();

        var changed = await userManager.ChangePasswordAsync(user, body.CurrentPassword, body.NewPassword);
        if (!changed.Succeeded)
        {
            var isPasswordMismatch = changed.Errors.Any(e => e.Code == nameof(IdentityErrorDescriber.PasswordMismatch));
            var message = isPasswordMismatch
                ? "Current password is incorrect."
                : string.Join(" ", changed.Errors.Select(e => e.Description));

            return Results.BadRequest(new { message });
        }

        user.MustChangePassword = false;
        var updated = await userManager.UpdateAsync(user);
        if (!updated.Succeeded)
        {
            var message = string.Join(" ", updated.Errors.Select(e => e.Description));
            return Results.BadRequest(new { message });
        }

        // Re-auth required after a password reset from temporary credentials.
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }
}