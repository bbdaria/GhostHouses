using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Dtos;

public record BuildingLogDto(
    int Id,
    int BuildingId,
    string Title,
    string Message,
    string Category,
    string Severity,
    DateTimeOffset CreatedAt,
    string? CreatedBy);

public class BuildingLogRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string Category { get; set; } = "general";
    public string Severity { get; set; } = "info";
}
