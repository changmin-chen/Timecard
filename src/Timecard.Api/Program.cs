using System.Diagnostics;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Features.AttendanceRequests;
using Timecard.Api.Features.Auth;
using Timecard.Api.Features.Calendar;
using Timecard.Api.Features.Days;
using Timecard.Api.Features.Month;
using Timecard.Api.Features.Punch;
using Timecard.Api.Features.Shared;
using Timecard.Api.Features.SyncPunch;
using Timecard.Api.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, services, loggerConfiguration) => loggerConfiguration
    .ReadFrom.Configuration(ctx.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

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

builder.Services
    .AddIdentityCore<AppUser>(options => {
        options.User.RequireUniqueEmail = true;
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 1;
        options.Stores.MaxLengthForKeys = 255;
    })
    .AddRoles<IdentityRole>()
    .AddSignInManager<SignInManager<AppUser>>()
    .AddEntityFrameworkStores<TimecardDb>();

builder.Services.AddAuthentication(IdentityConstants.ApplicationScheme)
    .AddIdentityCookies();

builder.Services.ConfigureApplicationCookie(options => {
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
    .AddPolicy("Admin", policy => policy.RequireRole(AuthRoles.Admin));

// --- Domain services ---

builder.Services.AddScoped<ICurrentUser, LocalCurrentUser>();
builder.Services.AddScoped<WorkDayRepository>();
builder.Services.AddScoped<IWorkCalendar, EfWorkCalendar>();
builder.Services.AddScoped<SyncPunchHandler>();
builder.Services.AddScoped<DgpaCalendarImporter>();
builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddProblemDetails(options => {
    options.CustomizeProblemDetails = ctx => {
        ctx.ProblemDetails.Extensions["traceId"] =
            Activity.Current?.Id ?? ctx.HttpContext.TraceIdentifier;
    };
});

var app = builder.Build();
var slowRequestThresholdMs = builder.Configuration.GetValue<double?>("RequestLogging:SlowRequestThresholdMs") ?? 1000;

{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<TimecardDb>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var seedLogger = loggerFactory.CreateLogger(nameof(DgpaCalendarSeed));

    await db.Database.MigrateAsync();
    await DgpaCalendarSeed.SeedAsync(db, seedLogger);

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
    var adminLogger = loggerFactory.CreateLogger("AdminSeed");

    if (!await roleManager.RoleExistsAsync(AuthRoles.Admin))
    {
        var created = await roleManager.CreateAsync(new IdentityRole(AuthRoles.Admin));
        if (!created.Succeeded)
        {
            var errors = string.Join("; ", created.Errors.Select(e => e.Description));
            adminLogger.LogError("Failed to create {Role}: {Errors}", AuthRoles.Admin, errors);
        }
    }

    // Seed initial admin account if Users table is empty.
    if (!await userManager.Users.AnyAsync(u => u.Id != "dev-placeholder"))
    {
        var email = app.Configuration["InitialAdmin:Email"];
        var employeeId = app.Configuration["InitialAdmin:EmployeeId"];
        var displayName = app.Configuration["InitialAdmin:DisplayName"];
        var password = app.Configuration["InitialAdmin:Password"];

        if (email is null || employeeId is null || password is null)
        {
            adminLogger.LogWarning(
                "InitialAdmin config is incomplete (Email/EmployeeId/Password required). " +
                "Skipping admin seed. Set 'InitialAdmin:Password' via user-secrets.");
        }
        else
        {
            var admin = new AppUser(email, employeeId, displayName)
            {
                MustChangePassword = true,
            };

            var created = await userManager.CreateAsync(admin, password);
            if (!created.Succeeded)
            {
                var errors = string.Join("; ", created.Errors.Select(e => e.Description));
                adminLogger.LogError("Failed to seed initial admin account: {Errors}", errors);
            }
            else
            {
                var roleAdded = await userManager.AddToRoleAsync(admin, AuthRoles.Admin);
                if (!roleAdded.Succeeded)
                {
                    var errors = string.Join("; ", roleAdded.Errors.Select(e => e.Description));
                    adminLogger.LogError("Failed to grant {Role} role to {Email}: {Errors}", AuthRoles.Admin, email, errors);
                }
                else
                {
                    adminLogger.LogInformation("Seeded initial admin account: {Email}", email);
                }
            }
        }
    }
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSerilogRequestLogging(options => {
    options.GetLevel = (httpContext, elapsed, ex) => {
        if (ex is not null || httpContext.Response.StatusCode >= StatusCodes.Status500InternalServerError)
            return LogEventLevel.Error;

        if (httpContext.Response.StatusCode >= StatusCodes.Status400BadRequest || elapsed >= slowRequestThresholdMs)
            return LogEventLevel.Warning;

        return LogEventLevel.Debug;
    };
});
app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapAdminEndpoints();
app.MapAdminExportEndpoints();
app.MapPunchEndpoints();
app.MapDayEndpoints();
app.MapAttendanceRequestEndpoints();
app.MapMonthEndpoints();
app.MapCalendarEndpoints();
app.MapSyncPunchEndpoints();

app.Run();
