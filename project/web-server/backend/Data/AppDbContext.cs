using Microsoft.EntityFrameworkCore;
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

        modelBuilder.Entity<Building>()
            .Property(b => b.FldId)
            .HasMaxLength(64);

        modelBuilder.Entity<BuildingLog>()
            .HasQueryFilter(log => !log.IsDeleted);
    }
}
