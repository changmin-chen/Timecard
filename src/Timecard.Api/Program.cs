using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Services;
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

var app = builder.Build();

{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<TimecardDb>();
    db.Database.Migrate();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPunchEndpoints();
app.MapDayEndpoints();
app.MapAttendanceRequestEndpoints();
app.MapMonthEndpoints();
app.MapCalendarEndpoints();

app.Run();
