using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Features.AttendanceRequests;
using Timecard.Api.Features.Auth;
using Timecard.Api.Features.Calendar;
using Timecard.Api.Features.Days;
using Timecard.Api.Features.Month;
using Timecard.Api.Features.Punch;
using Timecard.Api.Features.SyncPunch;
using Timecard.Api.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o => {
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.WriteIndented = true;
});

builder.Services.AddDbContext<TimecardDb>(opt => {
    var cs = builder.Configuration.GetConnectionString("Timecard")
             ?? throw new InvalidOperationException("ConnectionStrings:Timecard is missing.");
    opt.UseNpgsql(cs);
});

// --- Authentication & Authorization ---

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options => {
        // Return 401/403 for API routes instead of redirecting to a login page.
        options.Events.OnRedirectToLogin = ctx => {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx => {
            if (ctx.Request.Path.StartsWithSegments("/api"))
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

// All endpoints require authentication by default; auth endpoints opt out with AllowAnonymous().
builder.Services.AddAuthorizationBuilder()
    .SetFallbackPolicy(new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build())
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"));

// --- Domain services ---

builder.Services.AddSingleton<IPasswordHasher<AppUser>, PasswordHasher<AppUser>>();
builder.Services.AddScoped<ICurrentUser, LocalCurrentUser>();
builder.Services.AddScoped<WorkDayRepository>();
builder.Services.AddScoped<IWorkCalendar, EfWorkCalendar>();
builder.Services.AddScoped<DgpaCalendarImporter>();
builder.Services.AddProblemDetails(options => {
    options.CustomizeProblemDetails = ctx => {
        ctx.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();

{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TimecardDb>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var seedLogger = loggerFactory.CreateLogger(nameof(DgpaCalendarSeed));

    await db.Database.MigrateAsync();
    await DgpaCalendarSeed.SeedAsync(db, seedLogger);

    // Seed initial admin account if Users table is empty.
    if (!await db.Users.AnyAsync())
    {
        var adminLogger = loggerFactory.CreateLogger("AdminSeed");
        var email = app.Configuration["InitialAdmin:Email"];
        var displayName = app.Configuration["InitialAdmin:DisplayName"];
        var password = app.Configuration["InitialAdmin:Password"];

        if (email is null || displayName is null || password is null)
        {
            adminLogger.LogWarning(
                "InitialAdmin config is incomplete (Email/DisplayName/Password required). " +
                "Skipping admin seed. Set 'InitialAdmin:Password' via user-secrets.");
        }
        else
        {
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<AppUser>>();
            var admin = new AppUser
            {
                Id = Guid.NewGuid().ToString(),
                Email = email,
                DisplayName = displayName,
                IsAdmin = true,
                MustChangePassword = true,
            };
            admin.PasswordHash = hasher.HashPassword(admin, password);
            db.Users.Add(admin);
            await db.SaveChangesAsync();
            adminLogger.LogInformation("Seeded initial admin account: {Email}", email);
        }
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapPunchEndpoints();
app.MapDayEndpoints();
app.MapAttendanceRequestEndpoints();
app.MapMonthEndpoints();
app.MapCalendarEndpoints();
app.MapSyncPunchEndpoints();

app.Run();
