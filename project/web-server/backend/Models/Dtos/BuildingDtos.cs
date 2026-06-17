using System.ComponentModel.DataAnnotations;
using WebServer.Models;
using WebServer.Models.Users;

namespace WebServer.Models.Dtos;

public record BuildingFilterParameters(
    int Page = 1,
    int PageSize = 20,
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
    DateTime? UpdatedTo = null);

public record BuildingSummaryDto(
    int Id,
    int? StreetId,
    string BuildingName,
    string StreetName,
    string HouseNumber,
    string Neighborhood,
    BuildingStatus ShikumStatus,
    int? BldSivug,
    string StatusSummary,
    DateTime? StatusSummaryUpdatedAt,
    int? SugBaalut,
    string? Quarter,
    string? SubQuarter,
    string? StatisticalArea);

public record BuildingMapParameters(
    double? North = null,
    double? South = null,
    double? East = null,
    double? West = null,
    BuildingStatus? Status = null,
    int? BldSivug = null);

public record BuildingMapDto(
    int Id,
    int? StreetId,
    string BuildingName,
    string StreetName,
    string HouseNumber,
    string Neighborhood,
    BuildingStatus ShikumStatus,
    int? BldSivug,
    string StatusSummary,
    DateTime? StatusSummaryUpdatedAt,
    double Latitude,
    double Longitude);

public record BuildingDetailDto(
    BuildingSummaryDto Summary,
    string? StatusSummary,
    DateTime? StatusSummaryUpdatedAt,
    string? Complaints,
    string[] Photos,
    BuildingExternalDataDto ExternalData,
    IEnumerable<BuildingLogDto> RecentLogs,
    IEnumerable<BuildingFieldDto> Fields);

public record BuildingFieldDto(
    string Category,
    string FieldName,
    string ColumnName,
    string? SelectTableName,
    bool IncludeInEventLog,
    string? Value,
    int? RawValue);

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
    public int Id { get; set; }

    [Required]
    public int StreetId { get; set; }

    public string StreetName { get; set; } = string.Empty;

    [Required]
    public string HouseNumber { get; set; } = string.Empty;

    public string BuildingName { get; set; } = string.Empty;
    public string Neighborhood { get; set; } = string.Empty;
    public int? BldSivug { get; set; }
    public BuildingStatus? ShikumStatus { get; set; }
    public string? StatusSummary { get; set; }
    public string? Complaints { get; set; }
    public string[]? Photos { get; set; }
    public bool AllowDuplicate { get; set; }
}

public record DeleteBuildingRequest(
    string Reason,
    bool Confirm);

public record BuildingFieldsUpdateRequest(
    IDictionary<string, string?> Fields,
    bool AllowDuplicate = false);

public record PaginatedResult<T>(IEnumerable<T> Items, int Total, int Page, int PageSize);
