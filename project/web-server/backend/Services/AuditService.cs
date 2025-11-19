using System.Text.Json;
using WebServer.Data;
using WebServer.Models;

namespace WebServer.Services;

public interface IAuditService
{
    Task RecordAsync(Guid? userId, string entityType, string entityId, string action, object? changes = null, CancellationToken cancellationToken = default);
}

public class AuditService : IAuditService
{
    private readonly AppDbContext _context;

    public AuditService(AppDbContext context)
    {
        _context = context;
    }

    public async Task RecordAsync(Guid? userId, string entityType, string entityId, string action, object? changes = null, CancellationToken cancellationToken = default)
    {
        var entry = new AuditEntry
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            Changes = changes is null ? string.Empty : JsonSerializer.Serialize(changes),
            PerformedByUserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.AuditEntries.Add(entry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
