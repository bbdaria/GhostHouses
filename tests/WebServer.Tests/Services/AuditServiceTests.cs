using Microsoft.EntityFrameworkCore;
using WebServer.Data;
using WebServer.Services;
using WebServer.Models;

public class AuditServiceTests
{
    private AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task RecordAsync_CreatesAuditEntry()
    {
        var db = CreateDb();
        var service = new AuditService(db);

        await service.RecordAsync(
            userId: Guid.NewGuid(),
            entityType: "Building",
            entityId: "10",
            action: "Create",
            changes: new { Name = "Test" }
        );

        var entry = await db.AuditEntries.FirstOrDefaultAsync();

        Assert.NotNull(entry);
        Assert.Equal("Building", entry.EntityType);
        Assert.Equal("10", entry.EntityId);
        Assert.Equal("Create", entry.Action);
        Assert.False(string.IsNullOrWhiteSpace(entry.Changes));
        Assert.NotEqual(default, entry.CreatedAt);
    }
}
