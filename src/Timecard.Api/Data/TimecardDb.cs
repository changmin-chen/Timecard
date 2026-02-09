using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Timecard.Api.Data;

public sealed class TimecardDb(DbContextOptions<TimecardDb> options) : DbContext(options)
{
    public DbSet<WorkDay> WorkDays => Set<WorkDay>();
    public DbSet<PunchEvent> Punches => Set<PunchEvent>();
    public DbSet<Adjustment> Adjustments => Set<Adjustment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var dateOnlyConverter = new ValueConverter<DateOnly, string>(
            d => d.ToString("yyyy-MM-dd"),
            s => DateOnly.Parse(s));

        modelBuilder.Entity<WorkDay>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Date).HasConversion(dateOnlyConverter);
            e.HasIndex(x => x.Date).IsUnique();
            e.Property(x => x.Note).HasMaxLength(4000);

            e.HasMany(x => x.Punches)
                .WithOne()
                .HasForeignKey(x => x.WorkDayId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasMany(x => x.Adjustments)
                .WithOne()
                .HasForeignKey(x => x.WorkDayId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PunchEvent>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.WorkDayId, x.At });
            e.Property(x => x.Note).HasMaxLength(4000);
        });

        modelBuilder.Entity<Adjustment>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Kind).HasMaxLength(64);
            e.Property(x => x.Note).HasMaxLength(4000);
            e.HasIndex(x => new { x.WorkDayId, x.Kind });
        });
    }
}
