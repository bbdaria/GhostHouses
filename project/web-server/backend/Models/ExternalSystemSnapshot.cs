using WebServer.Utilities;

namespace WebServer.Models;

public class ExternalSystemSnapshot
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public string SystemName { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTimeOffset RetrievedAt { get; set; } = IsraelTime.NowUtc;
}
