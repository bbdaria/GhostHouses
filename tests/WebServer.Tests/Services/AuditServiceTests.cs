using Microsoft.EntityFrameworkCore;
using WebServer.Models;
using WebServer.Services;
using WebServer.Tests.TestSupport;

namespace WebServer.Tests.Services;

public class AuditServiceTests
{
    [Fact]
    public async Task RecordAsync_PersistsAuditEntryWithSerializedChanges()
    {
        await using var db = TestDb.Create();
        var service = new AuditService(db);
        var actorId = Guid.NewGuid();

        await service.RecordAsync(
            actorId,
            nameof(Building),
            "42",
            "Update",
            new { Field = "StatusSummary", Old = "old", New = "new" });

        var entry = await db.AuditEntries.SingleAsync();

        Assert.Equal(nameof(Building), entry.EntityType);
        Assert.Equal("42", entry.EntityId);
        Assert.Equal("Update", entry.Action);
        Assert.Equal(actorId, entry.PerformedByUserId);
        Assert.Contains("StatusSummary", entry.Changes);
        Assert.NotEqual(default, entry.CreatedAt);
    }
}
