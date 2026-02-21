using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;

namespace Timecard.Api.Features.Auth;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var g = app.MapGroup("/api/auth").WithTags("Auth").AllowAnonymous();

        g.MapGet("/login", Login);
        g.MapPost("/logout", (Delegate)Logout);
        g.MapGet("/me", Me).RequireAuthorization();

        return app;
    }

    // GET /api/auth/login?returnUrl=/
    // Initiates the Google OAuth2 redirect. After login, browser is sent to returnUrl.
    private static IResult Login(string? returnUrl)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl,
        };
        return Results.Challenge(props, [GoogleDefaults.AuthenticationScheme]);
    }

    // POST /api/auth/logout
    private static async Task<IResult> Logout(HttpContext http)
    {
        await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Ok();
    }

    // GET /api/auth/me — returns current user's identity claims
    private static IResult Me(ClaimsPrincipal user) =>
        Results.Ok(new
        {
            id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            email = user.FindFirst(ClaimTypes.Email)?.Value,
            name = user.FindFirst(ClaimTypes.Name)?.Value,
        });
}
