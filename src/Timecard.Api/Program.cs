using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Data;
using Timecard.Api.Features.Adjustments;
using Timecard.Api.Features.Days;
using Timecard.Api.Features.Month;
using Timecard.Api.Features.Punch;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.WriteIndented = true;
});

builder.Services.AddDbContext<TimecardDb>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Timecard") ?? "Data Source=App_Data/timecard_punch.db";
    opt.UseSqlite(cs);
});

builder.Services.AddScoped<WorkDayRepository>();

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TimecardDb>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapPunchEndpoints();
app.MapDayEndpoints();
app.MapAdjustmentEndpoints();
app.MapMonthEndpoints();

app.Run();
