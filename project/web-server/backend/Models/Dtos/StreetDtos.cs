using System.ComponentModel.DataAnnotations;

namespace WebServer.Models.Dtos;

public record StreetDto(int StreetId, string Name);

public class StreetEditRequest
{
    [Required]
    public int StreetId { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;
}
