using System.ComponentModel.DataAnnotations;
using WebServer.Models;
using WebServer.Models.Users;

namespace WebServer.Models.Dtos;

public record BuildingFilterParameters(
    int Page = 1,
    int PageSize = 20,
    string? Street = null,
    string? HouseNumber = null,
    string? Name = null,
    BuildingStatus? Status = null,
    string? Neighborhood = null,
    string? StatusSummary = null);

public record BuildingSummaryDto(
    int Id,
    string FldId,
    string BuildingName,
    string StreetName,
    string HouseNumber,
    string Neighborhood,
    BuildingStatus ShikumStatus,
    string BldSivug,
    string StatusSummary);

public record BuildingDetailDto(
    BuildingSummaryDto Summary,
    string? StatusSummary,
    DateTime? StatusSummaryUpdatedAt,
    string? Complaints,
    string[] Photos,
    BuildingExternalDataDto ExternalData,
    IEnumerable<BuildingLogDto> RecentLogs);

public record BuildingExternalDataDto(
    ExternalSystemSnapshotDto Gis,
    ExternalSystemSnapshotDto Water,
    ExternalSystemSnapshotDto Electricity,
    ExternalSystemSnapshotDto Tax,
    ExternalSystemSnapshotDto Complaints106);

public record ExternalSystemSnapshotDto(
    string SystemName,
    string Payload,
    DateTimeOffset RetrievedAt);

public record BuildingEditRequest
{
    [Required]
    public string FldId { get; set; } = string.Empty;

    [Required]
    public string StreetName { get; set; } = string.Empty;

    [Required]
    public string HouseNumber { get; set; } = string.Empty;

    public string BuildingName { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public string? BldSivug { get; set; }
    public BuildingStatus? ShikumStatus { get; set; }
    public string? StatusSummary { get; set; }
    public string? Complaints { get; set; }
    public string[]? Photos { get; set; }
}

public record DeleteBuildingRequest(
    string Reason,
    bool Confirm);

public record PaginatedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
