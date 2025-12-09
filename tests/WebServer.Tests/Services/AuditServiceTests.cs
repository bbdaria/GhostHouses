using WebServer.Services;
using WebServer.Data;
using WebServer.Models;
using Microsoft.EntityFrameworkCore;

public class AuditServiceTests
{
    private readonly AppDbContext _context;
    private readonly AuditService _service;

    public AuditServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase("audit-tests")
            .Options;

        _context = new AppDbContext(options);
        _service = new AuditService(_context);
    }

    [Fact]
    public async Task RecordAsync_CreatesAuditEntry()
    {
        // Act
        await _service.RecordAsync(
            userId: Guid.NewGuid(),
            entityType: "Building",
            entityId: "15",
            action: "Update",
            changes: new { Field = "Name", NewValue = "Test" }
        );

        // Assert
        var entry = await _context.AuditEntries.FirstOrDefaultAsync();
        Assert.NotNull(entry);
        Assert.Equal("Building", entry.EntityType);
        Assert.Equal("15", entry.EntityId);
        Assert.Equal("Update", entry.Action);
        Assert.False(string.IsNullOrWhiteSpace(entry.Changes));
    }
}
