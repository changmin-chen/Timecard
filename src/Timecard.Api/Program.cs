using System.Diagnostics;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Features.AttendanceRequests;
using Timecard.Api.Features.Auth;
using Timecard.Api.Features.Calendar;
using Timecard.Api.Features.Days;
using Timecard.Api.Features.Month;
using Timecard.Api.Features.Punch;
using Timecard.Api.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.WriteIndented = true;
});

builder.Services.AddDbContext<TimecardDb>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Timecard")
             ?? throw new InvalidOperationException("ConnectionStrings:Timecard is missing.");
    opt.UseNpgsql(cs);
});

// --- Authentication & Authorization ---

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // Return 401/403 for API routes instead of redirecting to a login page.
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientId is missing.");
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Authentication:Google:ClientSecret is missing.");

        // Just-in-time provision AppUser on first login, and keep name/email fresh.
        options.Events.OnCreatingTicket = async ctx =>
        {
            var userId = ctx.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var email = ctx.Principal?.FindFirst(ClaimTypes.Email)?.Value;
            var name = ctx.Principal?.FindFirst(ClaimTypes.Name)?.Value;

            if (userId is not null)
            {
                var db = ctx.HttpContext.RequestServices.GetRequiredService<TimecardDb>();
                var user = await db.Users.FindAsync(userId);
                if (user is null)
                {
                    db.Users.Add(new AppUser
                    {
                        Id = userId,
                        Email = email ?? "",
                        DisplayName = name ?? email ?? userId,
                    });
                }
                else
                {
                    user.Email = email ?? user.Email;
                    user.DisplayName = name ?? user.DisplayName;
                }
                await db.SaveChangesAsync();
            }
        };
    });

// All endpoints require authentication by default; auth endpoints opt out with AllowAnonymous().
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build());

// --- Domain services ---

builder.Services.AddScoped<ICurrentUser, GoogleCurrentUser>();
builder.Services.AddScoped<WorkDayRepository>();
builder.Services.AddScoped<IWorkCalendar, EfWorkCalendar>();
builder.Services.AddScoped<DgpaCalendarImporter>();
builder.Services.AddScoped<DgpaCalendarSeeder>();
builder.Services.Configure<DgpaCalendarSeedOptions>(
    builder.Configuration.GetSection("CalendarSeed:Dgpa"));
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = ctx =>
    {
        ctx.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TimecardDb>();
    var seeder = scope.ServiceProvider.GetRequiredService<DgpaCalendarSeeder>();

    await db.Database.MigrateAsync();
    await seeder.SeedAsync();
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapPunchEndpoints();
app.MapDayEndpoints();
app.MapAttendanceRequestEndpoints();
app.MapMonthEndpoints();
app.MapCalendarEndpoints();

app.Run();
