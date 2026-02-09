using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Timecard.Api.Data;
using Timecard.Api.Features.Adjustments;
using Timecard.Api.Features.Clock;
using Timecard.Api.Features.Days;
using Timecard.Api.Features.Month;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.WriteIndented = true;
});

builder.Services.AddDbContext<TimecardDb>(opt =>
{
    var cs = builder.Configuration.GetConnectionString("Timecard") ?? "Data Source=App_Data/timecard.db";
    opt.UseSqlite(cs);
});

var app = builder.Build();

// 讓你開箱即用：不需要先跑 migration（MVP 取捨）
// 如果你想正統：改成 db.Database.Migrate();
Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TimecardDb>();
    db.Database.EnsureCreated();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapClockEndpoints();
app.MapDayEndpoints();
app.MapAdjustmentEndpoints();
app.MapMonthEndpoints();

app.Run();
