using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using WebServer.Models;
using WebServer.Models.Users;

namespace WebServer.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<BuildingLog> BuildingLogs => Set<BuildingLog>();
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();
    public DbSet<ExternalSystemSnapshot> ExternalSystemSnapshots => Set<ExternalSystemSnapshot>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Username)
            .IsUnique();

        var statusConverter = new ValueConverter<BuildingStatus, string>(
            status => status.ToString(),
            value => ParseStatus(value));

        modelBuilder.Entity<Building>()
            .Property(b => b.FldId)
            .HasMaxLength(64);
        modelBuilder.Entity<Building>()
            .Property(b => b.ShikumStatus)
            .HasConversion(statusConverter)
            .HasMaxLength(64);

        modelBuilder.Entity<BuildingLog>()
            .HasQueryFilter(log => !log.IsDeleted);
    }

    private static BuildingStatus ParseStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return BuildingStatus.Unknown;
        }

        var normalized = value.Replace(" ", string.Empty);
        if (Enum.TryParse<BuildingStatus>(normalized, true, out var parsed))
        {
            return parsed;
        }

        return string.Equals(value, "Pending Survey", StringComparison.OrdinalIgnoreCase)
            ? BuildingStatus.UnderInspection
            : BuildingStatus.Unknown;
    }
}
