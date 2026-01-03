using System.Globalization;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;
using WebServer.Services;
using WebServer.Utilities;

namespace WebServer.Controllers;

[Route("api/[controller]")]
public class BuildingsController : ApiControllerBase
{
    private const int NoStreetId = -1;
    private const string NoStreetName = "ללא שם רחוב";

    private readonly AppDbContext _context;
    private readonly IExternalDataService _externalDataService;
    private readonly IAuditService _auditService;

    public BuildingsController(
        AppDbContext context,
        IExternalDataService externalDataService,
        IAuditService auditService)
    {
        _context = context;
        _externalDataService = externalDataService;
        _auditService = auditService;
    }

    [HttpGet]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<PaginatedResult<BuildingSummaryDto>>> GetBuildings(
        [FromQuery] BuildingFilterParameters filter,
        CancellationToken cancellationToken)
    {
        var query = _context.Buildings.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Street))
        {
            query = query.Where(b => EF.Functions.ILike(b.StreetName, $"%{filter.Street}%"));
        }

        if (filter.StreetId.HasValue)
        {
            if (filter.StreetId.Value == NoStreetId)
            {
                query = query.Where(b =>
                    b.StreetCode == NoStreetId ||
                    b.StreetCode == null ||
                    b.StreetName == NoStreetName);
            }
            else
            {
                query = query.Where(b => b.StreetCode == filter.StreetId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.HouseNumber))
        {
            query = query.Where(b => b.HouseNumber == filter.HouseNumber);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(b => EF.Functions.ILike(b.BuildingName, $"%{filter.Name}%"));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(b => b.ShikumStatus == filter.Status.Value);
        }

        if (filter.BldSivug.HasValue)
        {
            query = query.Where(b => b.BldSivug == filter.BldSivug.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
        {
            query = query.Where(b => EF.Functions.ILike(b.Neighborhood, $"%{filter.Neighborhood}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatusSummary))
        {
            query = query.Where(b => EF.Functions.ILike(b.StatusSummary, $"%{filter.StatusSummary}%"));
        }

        if (filter.SugBaalut.HasValue)
        {
            query = query.Where(b => b.SugBaalut == filter.SugBaalut.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Quarter))
        {
            query = query.Where(b => EF.Functions.ILike(b.Quarter, $"%{filter.Quarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.SubQuarter))
        {
            query = query.Where(b => EF.Functions.ILike(b.SubQuarter, $"%{filter.SubQuarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatisticalArea))
        {
            query = query.Where(b => EF.Functions.ILike(b.StatisticalArea, $"%{filter.StatisticalArea}%"));
        }

        if (filter.UpdatedFrom.HasValue)
        {
            var from = filter.UpdatedFrom.Value;
            if (from.TimeOfDay == TimeSpan.Zero)
            {
                from = from.Date;
            }

            query = query.Where(b => b.StatusSummaryUpdatedAt.HasValue && b.StatusSummaryUpdatedAt.Value >= from);
        }

        if (filter.UpdatedTo.HasValue)
        {
            var to = filter.UpdatedTo.Value;
            if (to.TimeOfDay == TimeSpan.Zero)
            {
                to = to.Date.AddDays(1).AddTicks(-1);
            }

            query = query.Where(b => b.StatusSummaryUpdatedAt.HasValue && b.StatusSummaryUpdatedAt.Value <= to);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(b => b.StreetName)
            .ThenBy(b => b.HouseNumber)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new BuildingSummaryDto(
                b.Id,
                b.FldId,
                b.StreetCode,
                b.BuildingName,
                b.Street != null ? b.Street.Name : b.StreetName,
                b.HouseNumber,
                b.Neighborhood,
                b.ShikumStatus,
                b.BldSivug,
                b.StatusSummary,
                IsraelTime.Convert(b.StatusSummaryUpdatedAt),
                b.SugBaalut,
                b.Quarter,
                b.SubQuarter,
                b.StatisticalArea))
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResult<BuildingSummaryDto>(items, total, filter.Page, filter.PageSize));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<BuildingDetailDto>> GetBuilding(int id, CancellationToken cancellationToken)
    {
        var building = await _context.Buildings
            .Include(b => b.Street)
            .Include(b => b.Logs.OrderByDescending(l => l.CreatedAt))
            .ThenInclude(l => l.CreatedByUser)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (building is null)
        {
            return NotFound();
        }

        var externalData = await _externalDataService.GetBuildingDataAsync(id, cancellationToken);
        var logs = building.Logs
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new BuildingLogDto(
                l.Id,
                l.BuildingId,
                l.Title,
                l.Message,
                l.Category,
                l.Severity,
                IsraelTime.Convert(l.CreatedAt),
                l.CreatedByUser?.Username,
                building.StreetName,
                building.HouseNumber,
                building.BuildingName,
                building.Neighborhood,
                building.BldSivug,
                building.ShikumStatus,
                building.StatusSummary))
            .ToList();

        var fields = BuildFieldsSnapshot(building);

        var detail = new BuildingDetailDto(
            new BuildingSummaryDto(
                building.Id,
                building.FldId,
                building.StreetCode,
                building.BuildingName,
                building.Street?.Name ?? building.StreetName,
                building.HouseNumber,
                building.Neighborhood,
                building.ShikumStatus,
                building.BldSivug,
                building.StatusSummary,
                IsraelTime.Convert(building.StatusSummaryUpdatedAt),
                building.SugBaalut,
                building.Quarter,
                building.SubQuarter,
                building.StatisticalArea),
            building.StatusSummary,
            IsraelTime.Convert(building.StatusSummaryUpdatedAt),
            building.Complaints,
            string.IsNullOrWhiteSpace(building.PhotoUrls) ? Array.Empty<string>() : building.PhotoUrls.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            externalData,
            logs,
            fields);

        return Ok(detail);
    }

    [HttpGet("export")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportBuildings(
        [FromQuery] BuildingFilterParameters filter,
        CancellationToken cancellationToken)
    {
        var query = _context.Buildings.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Street))
        {
            query = query.Where(b => EF.Functions.ILike(b.StreetName, $"%{filter.Street}%"));
        }

        if (filter.StreetId.HasValue)
        {
            if (filter.StreetId.Value == NoStreetId)
            {
                query = query.Where(b =>
                    b.StreetCode == NoStreetId ||
                    b.StreetCode == null ||
                    b.StreetName == NoStreetName);
            }
            else
            {
                query = query.Where(b => b.StreetCode == filter.StreetId.Value);
            }
        }

        if (!string.IsNullOrWhiteSpace(filter.HouseNumber))
        {
            query = query.Where(b => b.HouseNumber == filter.HouseNumber);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(b => EF.Functions.ILike(b.BuildingName, $"%{filter.Name}%"));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(b => b.ShikumStatus == filter.Status.Value);
        }

        if (filter.BldSivug.HasValue)
        {
            query = query.Where(b => b.BldSivug == filter.BldSivug.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
        {
            query = query.Where(b => EF.Functions.ILike(b.Neighborhood, $"%{filter.Neighborhood}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatusSummary))
        {
            query = query.Where(b => EF.Functions.ILike(b.StatusSummary, $"%{filter.StatusSummary}%"));
        }

        if (filter.SugBaalut.HasValue)
        {
            query = query.Where(b => b.SugBaalut == filter.SugBaalut.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Quarter))
        {
            query = query.Where(b => EF.Functions.ILike(b.Quarter, $"%{filter.Quarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.SubQuarter))
        {
            query = query.Where(b => EF.Functions.ILike(b.SubQuarter, $"%{filter.SubQuarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatisticalArea))
        {
            query = query.Where(b => EF.Functions.ILike(b.StatisticalArea, $"%{filter.StatisticalArea}%"));
        }

        if (filter.UpdatedFrom.HasValue)
        {
            var from = filter.UpdatedFrom.Value;
            if (from.TimeOfDay == TimeSpan.Zero)
            {
                from = from.Date;
            }

            query = query.Where(b => b.StatusSummaryUpdatedAt.HasValue && b.StatusSummaryUpdatedAt.Value >= from);
        }

        if (filter.UpdatedTo.HasValue)
        {
            var to = filter.UpdatedTo.Value;
            if (to.TimeOfDay == TimeSpan.Zero)
            {
                to = to.Date.AddDays(1).AddTicks(-1);
            }

            query = query.Where(b => b.StatusSummaryUpdatedAt.HasValue && b.StatusSummaryUpdatedAt.Value <= to);
        }

        var buildings = await query
            .OrderBy(b => b.StreetName)
            .ThenBy(b => b.HouseNumber)
            .ToListAsync(cancellationToken);

        var fieldDefinitions = BuildFieldsSnapshot(buildings.FirstOrDefault() ?? new Building());
        var groupedFields = fieldDefinitions
            .GroupBy(field => field.Category)
            .Select(group => new
            {
                Category = group.Key,
                Fields = group.ToList()
            })
            .ToList();

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Buildings");

        var columnIndex = 1;
        foreach (var group in groupedFields)
        {
            var startColumn = columnIndex;
            foreach (var field in group.Fields)
            {
                worksheet.Cell(2, columnIndex).Value = GetExcelAwareLabel(field.FieldName);
                columnIndex++;
            }

            if (columnIndex == startColumn)
            {
                continue;
            }

            var endColumn = columnIndex - 1;
            var categoryRange = worksheet.Range(1, startColumn, 1, endColumn);
            categoryRange.Merge();
            categoryRange.Value = group.Category;
        }

        worksheet.Row(1).Style.Font.Bold = true;
        worksheet.Row(2).Style.Font.Bold = true;

        for (var i = 0; i < buildings.Count; i++)
        {
            var building = buildings[i];
            var snapshot = BuildFieldsSnapshot(building);
            var valuesByColumn = snapshot
                .Where(field => !string.IsNullOrWhiteSpace(field.ColumnName))
                .ToDictionary(
                    field => field.ColumnName,
                    field => field.Value ?? string.Empty,
                    StringComparer.OrdinalIgnoreCase);

            var row = i + 3;
            for (var col = 0; col < fieldDefinitions.Count; col++)
            {
                var columnName = fieldDefinitions[col].ColumnName;
                valuesByColumn.TryGetValue(columnName, out var value);
                worksheet.Cell(row, col + 1).Value = value ?? string.Empty;
            }
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var fileName = $"buildings-{DateTimeOffset.UtcNow:yyyy-MM-dd}.xlsx";
        return File(
            stream.ToArray(),
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            fileName);
    }

    [HttpPost]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult<BuildingSummaryDto>> CreateBuilding([FromBody] BuildingEditRequest request, CancellationToken cancellationToken)
    {
        var building = new Building
        {
            FldId = request.FldId,
            HouseNumber = request.HouseNumber,
            BuildingName = request.BuildingName,
            Neighborhood = request.Neighborhood,
            BldSivug = request.BldSivug,
            ShikumStatus = request.ShikumStatus ?? BuildingStatus.Unknown,
            StatusSummary = request.StatusSummary ?? string.Empty,
            StatusSummaryUpdatedAt = DateTime.UtcNow,
            Complaints = request.Complaints ?? string.Empty,
            PhotoUrls = request.Photos is null ? string.Empty : string.Join(',', request.Photos)
        };

        if (request.StreetId == NoStreetId)
        {
            building.StreetCode = NoStreetId;
            building.StreetName = NoStreetName;
        }
        else
        {
            var street = await _context.Streets.FirstOrDefaultAsync(
                s => s.StreetId == request.StreetId,
                cancellationToken);
            if (street == null)
            {
                return BadRequest($"Street with id {request.StreetId} not found.");
            }

            building.StreetCode = street.StreetId;
            building.StreetName = street.Name;
        }

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync(cancellationToken);

        Guid? actorId = await ResolveActorIdAsync(cancellationToken);

        var fieldsSnapshot = BuildFieldsSnapshot(building);
        var externalData = await _externalDataService.GetBuildingDataAsync(building.Id, cancellationToken);
        var createChanges = BuildCreateChanges(building);
        var createSnapshot = new
        {
            building.Id,
            building.FldId,
            building.StreetCode,
            building.BuildingName,
            building.StreetName,
            building.HouseNumber,
            building.Neighborhood,
            building.BldSivug,
            building.ShikumStatus,
            building.StatusSummary,
            building.StatusSummaryUpdatedAt,
            Changes = createChanges,
            Fields = fieldsSnapshot,
            ExternalData = externalData
        };

        _context.BuildingLogs.Add(new BuildingLog
        {
            BuildingId = building.Id,
            Title = "יצירת מבנה",
            Message = JsonSerializer.Serialize(createSnapshot),
            Category = "Create",
            Severity = "info",
            CreatedByUserId = actorId,
            CreatedAt = IsraelTime.NowUtc
        });
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "Create", request, cancellationToken);

        return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, new BuildingSummaryDto(
            building.Id,
            building.FldId,
            building.StreetCode,
            building.BuildingName,
            building.StreetName,
            building.HouseNumber,
            building.Neighborhood,
            building.ShikumStatus,
            building.BldSivug,
            building.StatusSummary,
            IsraelTime.Convert(building.StatusSummaryUpdatedAt),
            building.SugBaalut,
            building.Quarter,
            building.SubQuarter,
            building.StatisticalArea));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult> UpdateBuilding(int id, [FromBody] BuildingEditRequest request, CancellationToken cancellationToken)
    {
        var building = await _context.Buildings.FindAsync(new object[] { id }, cancellationToken);
        if (building is null)
        {
            return NotFound();
        }

        var oldStreetName = building.StreetName;
        var oldHouseNumber = building.HouseNumber;
        var oldBuildingName = building.BuildingName;
        var oldBldSivug = building.BldSivug;
        var oldShikumStatus = building.ShikumStatus;
        var oldStatusSummary = building.StatusSummary;
        var oldStatusSummaryUpdatedAt = building.StatusSummaryUpdatedAt;

        building.FldId = request.FldId;
        building.HouseNumber = request.HouseNumber;
        if (!string.IsNullOrWhiteSpace(request.BuildingName))
        {
            building.BuildingName = request.BuildingName;
        }
        building.Neighborhood = request.Neighborhood;
        building.BldSivug = request.BldSivug ?? building.BldSivug;
        if (request.ShikumStatus.HasValue)
        {
            building.ShikumStatus = request.ShikumStatus.Value;
        }
        building.StatusSummary = request.StatusSummary ?? building.StatusSummary;
        building.Complaints = request.Complaints ?? building.Complaints;
        building.PhotoUrls = request.Photos is null ? building.PhotoUrls : string.Join(',', request.Photos);
        building.StatusSummaryUpdatedAt = DateTime.UtcNow;

        if (request.StreetId == NoStreetId)
        {
            building.StreetCode = NoStreetId;
            building.StreetName = NoStreetName;
        }
        else
        {
            var street = await _context.Streets.FirstOrDefaultAsync(
                s => s.StreetId == request.StreetId,
                cancellationToken);
            if (street == null)
            {
                return BadRequest($"Street with id {request.StreetId} not found.");
            }

            building.StreetCode = street.StreetId;
            building.StreetName = street.Name;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var fieldsSnapshot = BuildFieldsSnapshot(building);
        var externalData = await _externalDataService.GetBuildingDataAsync(building.Id, cancellationToken);
        var changeSnapshot = new
        {
            building.Id,
            building.FldId,
            building.StreetCode,
            building.BuildingName,
            building.StreetName,
            building.HouseNumber,
            building.Neighborhood,
            building.BldSivug,
            building.ShikumStatus,
            building.StatusSummary,
            building.StatusSummaryUpdatedAt,
            Changes = BuildCoreChanges(
                oldStreetName,
                oldHouseNumber,
                oldBuildingName,
                oldBldSivug,
                oldShikumStatus,
                oldStatusSummary,
                oldStatusSummaryUpdatedAt,
                building),
            Fields = fieldsSnapshot,
            ExternalData = externalData
        };

        Guid? actorId = await ResolveActorIdAsync(cancellationToken);

        _context.BuildingLogs.Add(new BuildingLog
        {
            BuildingId = building.Id,
            Title = "עדכון מבנה",
            Message = JsonSerializer.Serialize(changeSnapshot),
            Category = "Edit",
            Severity = "info",
            CreatedByUserId = actorId,
            CreatedAt = IsraelTime.NowUtc
        });
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "Update", request, cancellationToken);
        return NoContent();
    }

    [HttpPut("{id:int}/fields")]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult<BuildingDetailDto>> UpdateBuildingFields(
        int id,
        [FromBody] BuildingFieldsUpdateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Fields is null || request.Fields.Count == 0)
        {
            return BadRequest("No fields supplied.");
        }

        var building = await _context.Buildings
            .Include(b => b.Street)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (building is null)
        {
            return NotFound();
        }

        var propertyByColumn = typeof(Building)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite)
            .Select(p => new
            {
                Property = p,
                Column = p.GetCustomAttribute<ColumnAttribute>()
            })
            .Where(x => x.Column is not null)
            .ToDictionary(
                x => string.IsNullOrWhiteSpace(x.Column!.Name) ? x.Property.Name : x.Column!.Name!,
                x => x.Property,
                StringComparer.OrdinalIgnoreCase);

        bool streetIdProvided = false;
        int? desiredStreetId = null;

        var originalStreetName = building.StreetName;
        var changes = new List<FieldChange>();

        foreach (var (columnName, rawValue) in request.Fields)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                continue;
            }

            if (string.Equals(columnName, "StreetName", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.Equals(columnName, "StreetId", StringComparison.OrdinalIgnoreCase))
            {
                streetIdProvided = true;
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    desiredStreetId = null;
                }
                else if (int.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var sid))
                {
                    desiredStreetId = sid;
                }
                else
                {
                    return BadRequest("StreetId must be an integer.");
                }

                continue;
            }

            if (!propertyByColumn.TryGetValue(columnName, out var property))
            {
                continue;
            }

            var oldValue = property.GetValue(building);
            var converted = ConvertFieldValue(rawValue, property);
            if (converted is InvalidFieldValue invalid)
            {
                return BadRequest($"Invalid value for '{columnName}': {invalid.Message}");
            }

            var change = BuildChange(property, oldValue, converted);
            if (change is not null)
            {
                changes.Add(change);
            }
            property.SetValue(building, converted);
        }

        if (streetIdProvided)
        {
            if (!desiredStreetId.HasValue)
            {
                return BadRequest("StreetId is required.");
            }

            if (desiredStreetId.Value == NoStreetId)
            {
                building.StreetCode = NoStreetId;
                building.StreetName = NoStreetName;
            }
            else
            {
                var street = await _context.Streets.FirstOrDefaultAsync(
                    s => s.StreetId == desiredStreetId.Value,
                    cancellationToken);
                if (street is null)
                {
                    return BadRequest($"Street with id {desiredStreetId.Value} not found.");
                }

                building.StreetCode = street.StreetId;
                building.StreetName = street.Name;
            }
        }

        var hasChanges = changes.Count > 0 ||
            !string.Equals(originalStreetName, building.StreetName, StringComparison.Ordinal);
        if (hasChanges)
        {
            building.StatusSummaryUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (!string.Equals(originalStreetName, building.StreetName, StringComparison.Ordinal))
        {
            var streetNameProperty = propertyByColumn.TryGetValue("StreetName", out var property)
                ? property
                : typeof(Building).GetProperty(nameof(Building.StreetName));
            if (streetNameProperty is not null)
            {
                var change = BuildChange(streetNameProperty, originalStreetName, building.StreetName);
                if (change is not null)
                {
                    changes.Add(change);
                }
            }
        }

        var fieldsSnapshot = BuildFieldsSnapshot(building);
        var externalData = await _externalDataService.GetBuildingDataAsync(building.Id, cancellationToken);
        Guid? actorId = await ResolveActorIdAsync(cancellationToken);
        _context.BuildingLogs.Add(new BuildingLog
        {
            BuildingId = building.Id,
            Title = "עדכון שדות",
            Message = JsonSerializer.Serialize(new
            {
                building.Id,
                building.FldId,
                building.StreetCode,
                building.BuildingName,
                building.StreetName,
                building.HouseNumber,
                building.Neighborhood,
                building.BldSivug,
                building.ShikumStatus,
                building.StatusSummary,
                building.StatusSummaryUpdatedAt,
                Changes = changes,
                Fields = fieldsSnapshot,
                ExternalData = externalData
            }),
            Category = "Edit",
            Severity = "info",
            CreatedByUserId = actorId,
            CreatedAt = IsraelTime.NowUtc
        });
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "UpdateFields", request, cancellationToken);

        return await GetBuilding(id, cancellationToken);
    }

    private sealed record InvalidFieldValue(string Message);

    private sealed record FieldChange(string ColumnName, string FieldName, string? OldValue, string? NewValue);

    private static IReadOnlyList<BuildingFieldDto> BuildFieldsSnapshot(Building building)
    {
        return typeof(Building)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .Where(p =>
            {
                if (p.Name is nameof(Building.Logs) or nameof(Building.ExternalSnapshots) or nameof(Building.Street))
                {
                    return false;
                }

                if (p.Name is nameof(Building.Neighborhood) or nameof(Building.PhotoUrls))
                {
                    return false;
                }

                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) &&
                    p.PropertyType != typeof(string))
                {
                    return false;
                }

                return p.GetCustomAttribute<ColumnAttribute>() is not null;
            })
            .Select(p =>
            {
                var columnAttribute = p.GetCustomAttribute<ColumnAttribute>();
                var columnName = string.IsNullOrWhiteSpace(columnAttribute?.Name) ? p.Name : columnAttribute!.Name!;
                var fieldSpec = p.GetCustomAttribute<FieldSpecAttribute>();
                var displayAttribute = p.GetCustomAttribute<DisplayAttribute>();

                var category = fieldSpec?.Category ?? "כללי";
                var fieldName = fieldSpec?.FieldName ?? displayAttribute?.Name ?? p.Name;
                var selectTableName = fieldSpec?.SelectTableName;
                var includeInEventLog = fieldSpec?.IncludeInEventLog ?? false;

                var raw = p.GetValue(building);
                int? rawInt = raw switch
                {
                    null => null,
                    int i => i,
                    BuildingStatus s => (int)s,
                    _ => null
                };

                string? value = null;
                if (raw is null)
                {
                    value = null;
                }
                else if (!string.IsNullOrWhiteSpace(selectTableName) && rawInt.HasValue)
                {
                    var label = SelectTables
                        .GetOptions(selectTableName)
                        .FirstOrDefault(o => o.Value == rawInt.Value)
                        ?.Label;
                    value = label ?? raw.ToString();
                }
                else if (raw is DateTime dt)
                {
                    value = dt.ToString("yyyy-MM-dd");
                }
                else if (raw is DateTimeOffset dto)
                {
                    value = dto.ToString("O");
                }
                else
                {
                    value = raw.ToString();
                }

                return new BuildingFieldDto(
                    category,
                    fieldName,
                    columnName,
                    selectTableName,
                    includeInEventLog,
                    value,
                    rawInt);
            })
            .OrderBy(f => f.Category)
            .ThenBy(f => f.FieldName)
            .ToList();
    }

    private static string GetExcelAwareLabel(string fieldName)
    {
        if (string.IsNullOrWhiteSpace(fieldName))
        {
            return string.Empty;
        }

        var overrides = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["ID נכס לצורך מערכת זו בלבד"] = "ID",
            ["תמצית מצב"] = "תמונת מצב",
            ["תאריך עדכון תמצית מצב"] = "תאריך עדכון סטטוס",
            ["ציון עמידה בסטנדרט"] = "ציון",
            ["פרטי מחזיקים"] = "פרטי מחזיק",
            ["האם הייתה צריכת מים ב־6 החודשים האחרונים"] = "צריכת מים ב-6 החודשים האחרונים",
            ["האם הייתה צריכת חשמל ב־6 החודשים האחרונים"] = "צריכת חשמל ב-6 החודשים האחרונים",
            ["אחוז המבנה שמוגדר ניזוק"] = "אחוז המבנה שעומד ניזוק",
            ["קוארדינטות אורך"] = "קוארדינטות",
            ["קוארדינטות רוחב"] = "קוארדינטות"
        };

        if (!overrides.TryGetValue(fieldName, out var excelName) || excelName == fieldName)
        {
            return fieldName;
        }

        if (excelName == "קוארדינטות")
        {
            if (fieldName.Contains("אורך", StringComparison.Ordinal))
            {
                return "קוארדינטות (אורך)";
            }

            if (fieldName.Contains("רוחב", StringComparison.Ordinal))
            {
                return "קוארדינטות (רוחב)";
            }

            return excelName;
        }

        return $"{excelName} ({fieldName})";
    }

    private static IReadOnlyList<FieldChange> BuildCreateChanges(Building building)
    {
        var changes = new List<FieldChange>();
        AddChangeIfSet(changes, typeof(Building).GetProperty(nameof(Building.StreetName)), null, building.StreetName);
        AddChangeIfSet(changes, typeof(Building).GetProperty(nameof(Building.HouseNumber)), null, building.HouseNumber);
        AddChangeIfSet(changes, typeof(Building).GetProperty(nameof(Building.BuildingName)), null, building.BuildingName);
        AddChangeIfSet(changes, typeof(Building).GetProperty(nameof(Building.ShikumStatus)), null, building.ShikumStatus);
        AddChangeIfSet(changes, typeof(Building).GetProperty(nameof(Building.BldSivug)), null, building.BldSivug);
        AddChangeIfSet(changes, typeof(Building).GetProperty(nameof(Building.StatusSummary)), null, building.StatusSummary);
        AddChangeIfSet(
            changes,
            typeof(Building).GetProperty(nameof(Building.StatusSummaryUpdatedAt)),
            null,
            building.StatusSummaryUpdatedAt);
        return changes;
    }

    private static IReadOnlyList<FieldChange> BuildCoreChanges(
        string oldStreetName,
        string oldHouseNumber,
        string oldBuildingName,
        int? oldBldSivug,
        BuildingStatus oldShikumStatus,
        string oldStatusSummary,
        DateTime? oldStatusSummaryUpdatedAt,
        Building building)
    {
        var changes = new List<FieldChange>();
        AddChange(changes, typeof(Building).GetProperty(nameof(Building.StreetName)), oldStreetName, building.StreetName);
        AddChange(changes, typeof(Building).GetProperty(nameof(Building.HouseNumber)), oldHouseNumber, building.HouseNumber);
        AddChange(changes, typeof(Building).GetProperty(nameof(Building.BuildingName)), oldBuildingName, building.BuildingName);
        AddChange(changes, typeof(Building).GetProperty(nameof(Building.ShikumStatus)), oldShikumStatus, building.ShikumStatus);
        AddChange(changes, typeof(Building).GetProperty(nameof(Building.BldSivug)), oldBldSivug, building.BldSivug);
        AddChange(changes, typeof(Building).GetProperty(nameof(Building.StatusSummary)), oldStatusSummary, building.StatusSummary);
        AddChange(
            changes,
            typeof(Building).GetProperty(nameof(Building.StatusSummaryUpdatedAt)),
            oldStatusSummaryUpdatedAt,
            building.StatusSummaryUpdatedAt);
        return changes;
    }

    private static void AddChange(List<FieldChange> changes, PropertyInfo? property, object? oldValue, object? newValue)
    {
        if (property is null || ValuesEqual(oldValue, newValue))
        {
            return;
        }

        var change = BuildChange(property, oldValue, newValue);
        if (change is not null)
        {
            changes.Add(change);
        }
    }

    private static void AddChangeIfSet(
        List<FieldChange> changes,
        PropertyInfo? property,
        object? oldValue,
        object? newValue)
    {
        if (property is null || IsEmptyValue(newValue))
        {
            return;
        }

        var change = BuildChange(property, oldValue, newValue);
        if (change is not null)
        {
            changes.Add(change);
        }
    }

    private static FieldChange? BuildChange(PropertyInfo property, object? oldValue, object? newValue)
    {
        if (ValuesEqual(oldValue, newValue))
        {
            return null;
        }

        var columnName = GetColumnName(property);
        var fieldName = GetFieldName(property);
        return new FieldChange(
            columnName,
            fieldName,
            FormatFieldValue(property, oldValue),
            FormatFieldValue(property, newValue));
    }

    private static string GetColumnName(PropertyInfo property)
    {
        var columnAttribute = property.GetCustomAttribute<ColumnAttribute>();
        return string.IsNullOrWhiteSpace(columnAttribute?.Name) ? property.Name : columnAttribute!.Name!;
    }

    private static string GetFieldName(PropertyInfo property)
    {
        var fieldSpec = property.GetCustomAttribute<FieldSpecAttribute>();
        var display = property.GetCustomAttribute<DisplayAttribute>();
        return fieldSpec?.FieldName ?? display?.Name ?? property.Name;
    }

    private static string? FormatFieldValue(PropertyInfo property, object? value)
    {
        if (value is null)
        {
            return null;
        }

        var fieldSpec = property.GetCustomAttribute<FieldSpecAttribute>();
        var selectTableName = fieldSpec?.SelectTableName?.Trim();
        int? rawInt = value switch
        {
            int i => i,
            BuildingStatus s => (int)s,
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(selectTableName) && rawInt.HasValue)
        {
            var label = SelectTables
                .GetOptions(selectTableName)
                .FirstOrDefault(o => o.Value == rawInt.Value)
                ?.Label;
            return label ?? value.ToString();
        }

        if (value is DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
        }

        if (value is DateTimeOffset dto)
        {
            return dto.ToString("O");
        }

        return value.ToString();
    }

    private static bool ValuesEqual(object? oldValue, object? newValue)
    {
        if (IsEmptyValue(oldValue) && IsEmptyValue(newValue))
        {
            return true;
        }

        return Equals(oldValue, newValue);
    }

    private static bool IsEmptyValue(object? value)
    {
        return value is null || value is string text && string.IsNullOrWhiteSpace(text);
    }

    private static object? ConvertFieldValue(string? raw, PropertyInfo property)
    {
        var targetType = property.PropertyType;
        var underlying = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (string.IsNullOrWhiteSpace(raw))
        {
            if (underlying == typeof(string))
            {
                return string.Empty;
            }

            if (targetType.IsValueType && Nullable.GetUnderlyingType(targetType) is null)
            {
                return Activator.CreateInstance(targetType);
            }

            return null;
        }

        raw = raw.Trim();

        if (underlying == typeof(string))
        {
            return raw;
        }

        if (underlying == typeof(int))
        {
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var i))
            {
                return i;
            }

            var fieldSpec = property.GetCustomAttribute<FieldSpecAttribute>();
            var selectTableName = fieldSpec?.SelectTableName?.Trim();
            if (!string.IsNullOrWhiteSpace(selectTableName))
            {
                var normalizedRaw = raw.Replace("\"\"", "\"");
                var option = SelectTables
                    .GetOptions(selectTableName)
                    .FirstOrDefault(o => string.Equals(o.Label?.Trim().Replace("\"\"", "\""), normalizedRaw, StringComparison.Ordinal));
                if (option != null)
                {
                    return option.Value;
                }
            }

            return new InvalidFieldValue("expected integer.");
        }

        if (underlying == typeof(double))
        {
            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
            {
                return d;
            }

            return new InvalidFieldValue("expected number.");
        }

        if (underlying == typeof(decimal))
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
            {
                return dec;
            }

            return new InvalidFieldValue("expected decimal.");
        }

        if (underlying == typeof(Money))
        {
            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var amount))
            {
                return new Money(amount);
            }

            return new InvalidFieldValue("expected money decimal.");
        }

        if (underlying == typeof(DateTime))
        {
            if (DateTime.TryParse(raw, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            {
                return dt.Date;
            }

            if (double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var oa))
            {
                try
                {
                    return DateTime.FromOADate(oa).Date;
                }
                catch
                {
                    // ignored
                }
            }

            return new InvalidFieldValue("expected date.");
        }

        if (underlying.IsEnum)
        {
            if (int.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var enumInt))
            {
                try
                {
                    return Enum.ToObject(underlying, enumInt);
                }
                catch
                {
                    return new InvalidFieldValue("invalid enum integer.");
                }
            }

            var fieldSpec = property.GetCustomAttribute<FieldSpecAttribute>();
            var selectTableName = fieldSpec?.SelectTableName?.Trim();
            if (!string.IsNullOrWhiteSpace(selectTableName))
            {
                var normalizedRaw = raw.Replace("\"\"", "\"");
                var option = SelectTables
                    .GetOptions(selectTableName)
                    .FirstOrDefault(o => string.Equals(o.Label?.Trim().Replace("\"\"", "\""), normalizedRaw, StringComparison.Ordinal));
                if (option != null)
                {
                    return Enum.ToObject(underlying, option.Value);
                }
            }

            try
            {
                return Enum.Parse(underlying, raw, ignoreCase: true);
            }
            catch
            {
                return new InvalidFieldValue("expected enum value.");
            }
        }

        return new InvalidFieldValue("unsupported field type.");
    }

    private async Task<Guid?> ResolveActorIdAsync(CancellationToken cancellationToken)
    {
        var actorId = CurrentUserId;
        if (actorId.HasValue)
        {
            var exists = await _context.Users.AnyAsync(u => u.Id == actorId.Value, cancellationToken);
            if (!exists)
            {
                return null;
            }
        }

        return actorId;
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult> DeleteBuilding(int id, [FromBody] DeleteBuildingRequest request, CancellationToken cancellationToken)
    {
        if (!request.Confirm)
        {
            return BadRequest("Deletion requires confirmation.");
        }

        var building = await _context.Buildings.FindAsync(new object[] { id }, cancellationToken);
        if (building is null)
        {
            return NotFound();
        }

        var hasCriticalLogs = await _context.BuildingLogs.AnyAsync(l => l.BuildingId == id && l.Severity == "critical", cancellationToken);
        if (hasCriticalLogs)
        {
            return Conflict("Building has critical logs and cannot be deleted.");
        }

        _context.Buildings.Remove(building);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), id.ToString(), "Delete", new { request.Reason }, cancellationToken);

        return NoContent();
    }
}
