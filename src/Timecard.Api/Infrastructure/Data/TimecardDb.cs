using Microsoft.EntityFrameworkCore;
using Timecard.Api.Domain.Entities;
using Timecard.Api.Domain.Entities.WorkDayAggregate;

namespace Timecard.Api.Infrastructure.Data;

public sealed class TimecardDb(DbContextOptions<TimecardDb> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<WorkDay> WorkDays => Set<WorkDay>();
    public DbSet<PunchEvent> Punches => Set<PunchEvent>();
    public DbSet<AttendanceRequest> AttendanceRequests => Set<AttendanceRequest>();
    public DbSet<CalendarDay> CalendarDays => Set<CalendarDay>();
    public DbSet<CalendarDayOverride> CalendarDayOverrides => Set<CalendarDayOverride>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasMaxLength(255);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.DisplayName).HasMaxLength(255);
            e.Property(x => x.EmployeeId).HasMaxLength(64);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.EmployeeId);
        });

        modelBuilder.Entity<WorkDay>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(255);
            e.Property(x => x.Date).HasColumnType("date");
            e.HasIndex(x => new { x.UserId, x.Date }).IsUnique();

            e.HasOne<AppUser>()
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

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

            e.ComplexProperty(x => x.Range, b => {
                b.Property(p => p.Start)
                    .HasColumnName("Start")
                    .HasColumnType("time without time zone");
                b.Property(p => p.End)
                    .HasColumnName("End")
                    .HasColumnType("time without time zone");
            });
            
            e.HasIndex(x => new { x.WorkDayId, x.Category });

            // Keep bad data out of the DB.
            e.ToTable(t => t.HasCheckConstraint(
                "CK_AttendanceRequests_StartBeforeEnd",
                "\"End\" > \"Start\""));
        });

        modelBuilder.Entity<CalendarDay>(e =>
        {
            e.HasKey(x => new { x.CalendarId, x.Date });
            e.Property(x => x.CalendarId).HasMaxLength(64);
            e.Property(x => x.Date).HasColumnType("date");
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
            e.Property(x => x.Date).HasColumnType("date");
            e.Property(x => x.Kind).HasMaxLength(64);
            e.Property(x => x.Note).HasMaxLength(4000);
            e.Property(x => x.Source).HasMaxLength(64);
            e.Property(x => x.UpdatedAt);
            e.HasIndex(x => x.Date);
        });
    }
}
