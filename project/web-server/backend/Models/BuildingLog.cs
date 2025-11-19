using WebServer.Models.Users;

namespace WebServer.Models;

public class BuildingLog
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public Guid? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
