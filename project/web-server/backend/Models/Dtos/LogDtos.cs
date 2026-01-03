using System.ComponentModel.DataAnnotations;
using WebServer.Models;

namespace WebServer.Models.Dtos;

public record LogFilterParameters(
    int Page = 1,
    int PageSize = 20,
    int? BuildingId = null,
    Guid? UserId = null,
    string? User = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    string? Street = null,
    int? StreetId = null,
    string? HouseNumber = null,
    string? Name = null,
    BuildingStatus? Status = null,
    int? BldSivug = null,
    string? Neighborhood = null,
    string? StatusSummary = null,
    int? SugBaalut = null,
    string? Quarter = null,
    string? SubQuarter = null,
    string? StatisticalArea = null,
    DateTime? UpdatedFrom = null,
    DateTime? UpdatedTo = null) : BuildingFilterParameters(
        Page,
        PageSize,
        Street,
        StreetId,
        HouseNumber,
        Name,
        Status,
        BldSivug,
        Neighborhood,
        StatusSummary,
        SugBaalut,
        Quarter,
        SubQuarter,
        StatisticalArea,
        UpdatedFrom,
        UpdatedTo);

public record BuildingLogDto(
    int Id,
    int BuildingId,
    string Title,
    string Message,
    string Category,
    string Severity,
    DateTimeOffset CreatedAt,
    string? CreatedBy,
    string? BuildingStreet,
    string? BuildingHouseNumber,
    string? BuildingNickname,
    string? BuildingNeighborhood,
    int? BuildingBldSivug,
    BuildingStatus? BuildingStatus,
    string? BuildingStatusSummary,
    int? BuildingSugBaalut,
    string? BuildingQuarter,
    string? BuildingSubQuarter,
    string? BuildingStatisticalArea);

public class BuildingLogRequest
{
    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    public string Category { get; set; } = "general";
    public string Severity { get; set; } = "info";
}
