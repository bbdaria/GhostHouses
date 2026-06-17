using WebServer.Models.Users;
using WebServer.Utilities;

namespace WebServer.Models;

public class BuildingLog
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public AppUser? CreatedByUser { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Severity { get; set; } = "info";
    public DateTimeOffset CreatedAt { get; set; } = IsraelTime.NowUtc;
    public DateTimeOffset? UpdatedAt { get; set; }
}
