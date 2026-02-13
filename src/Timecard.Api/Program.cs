using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Features.AttendanceRequests;
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

app.MapPunchEndpoints();
app.MapDayEndpoints();
app.MapAttendanceRequestEndpoints();
app.MapMonthEndpoints();
app.MapCalendarEndpoints();

app.Run();
