using Microsoft.EntityFrameworkCore;
using WebServer.Models;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using WebServer.Models.Users;

namespace WebServer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Street> Streets => Set<Street>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<BuildingLog> BuildingLogs => Set<BuildingLog>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        var moneyConverter = new ValueConverter<Money?, decimal?>(
            v => v.HasValue ? v.Value.Amount : null,
            v => v.HasValue ? new Money(v.Value) : null);

        var moneyComparer = new ValueComparer<Money?>(
            (left, right) =>
                left.HasValue == right.HasValue &&
                (!left.HasValue || left.Value.Amount == right!.Value.Amount),
            value => value.HasValue ? value.Value.Amount.GetHashCode() : 0,
            value => value);

        modelBuilder.Entity<Building>()
            .Property(b => b.ArnonaDept)
            .HasConversion(moneyConverter)
            .Metadata.SetValueComparer(moneyComparer);

        modelBuilder.Entity<BuildingLog>()
            .HasIndex(log => log.BuildingId);

        modelBuilder.Entity<Street>()
            .HasKey(s => s.StreetId);

        modelBuilder.Entity<Street>()
            .HasIndex(s => s.Name);

        modelBuilder.Entity<Building>()
            .HasOne(b => b.Street)
            .WithMany(s => s.Buildings)
            .HasForeignKey(b => b.StreetCode)
            .OnDelete(DeleteBehavior.SetNull);
    }

}
