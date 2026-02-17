using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Entities.WorkDayAggregate;

namespace Timecard.Api.Infrastructure.Data;

public sealed class TimecardDb(DbContextOptions<TimecardDb> options) : DbContext(options)
{
    public DbSet<WorkDay> WorkDays => Set<WorkDay>();
    public DbSet<PunchEvent> Punches => Set<PunchEvent>();
    public DbSet<AttendanceRequest> AttendanceRequests => Set<AttendanceRequest>();
    public DbSet<CalendarDay> CalendarDays => Set<CalendarDay>();
    public DbSet<CalendarDayOverride> CalendarDayOverrides => Set<CalendarDayOverride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            d => d.ToString("yyyy-MM-dd"),
            s => DateOnly.Parse(s));

        var timeOnlyConverter = new ValueConverter<TimeOnly, string>(
            t => t.ToString("HH:mm"),
            s => TimeOnly.Parse(s));

        modelBuilder.Entity<WorkDay>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Date).HasConversion(dateOnlyConverter);
            e.HasIndex(x => x.Date).IsUnique();

            e.Metadata.FindNavigation(nameof(WorkDay.Punches))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);
            e.Metadata.FindNavigation(nameof(WorkDay.AttendanceRequests))!
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            e.HasMany(x => x.Punches)
                .WithOne()
                .HasForeignKey(x => x.WorkDayId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.AttendanceRequests)
                .WithOne()
                .HasForeignKey(x => x.WorkDayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PunchEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.WorkDayId, x.At });
            e.Property(x => x.Note).HasMaxLength(4000);
            e.Property(x => x.WorkDayId);
            e.Property(x => x.At);
        });

        modelBuilder.Entity<AttendanceRequest>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Category).HasMaxLength(64);
            e.Property(x => x.Note).HasMaxLength(4000);
            e.Property(x => x.WorkDayId);
            e.Property(x => x.Start).HasConversion(timeOnlyConverter);
            e.Property(x => x.End).HasConversion(timeOnlyConverter);
            e.HasIndex(x => new { x.WorkDayId, x.Category });
        });

        modelBuilder.Entity<CalendarDay>(e =>
        {
            e.HasKey(x => new { x.CalendarId, x.Date });
            e.Property(x => x.CalendarId).HasMaxLength(64);
            e.Property(x => x.Date).HasConversion(dateOnlyConverter);
            e.Property(x => x.Kind).HasMaxLength(64);
            e.Property(x => x.Note).HasMaxLength(4000);
            e.Property(x => x.Source).HasMaxLength(64);
            e.Property(x => x.VersionImportedAt);
            e.HasIndex(x => x.Date);
        });

        modelBuilder.Entity<CalendarDayOverride>(e =>
        {
            e.HasKey(x => new { x.CalendarId, x.Date });
            e.Property(x => x.CalendarId).HasMaxLength(64);
            e.Property(x => x.Date).HasConversion(dateOnlyConverter);
            e.Property(x => x.Kind).HasMaxLength(64);
            e.Property(x => x.Note).HasMaxLength(4000);
            e.Property(x => x.Source).HasMaxLength(64);
            e.Property(x => x.UpdatedAt);
            e.HasIndex(x => x.Date);
        });
    }
}
