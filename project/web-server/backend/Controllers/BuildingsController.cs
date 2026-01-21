using System.Globalization;
using System.IO.Compression;
using System.Text.Json;
using System.Xml.Linq;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
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
    private readonly IWebHostEnvironment _hostEnvironment;

    public BuildingsController(
        AppDbContext context,
        IExternalDataService externalDataService,
        IAuditService auditService,
        IWebHostEnvironment hostEnvironment)
    {
        _context = context;
        _externalDataService = externalDataService;
        _auditService = auditService;
        _hostEnvironment = hostEnvironment;
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
            query = query.Where(b => EF.Functions.ILike(b.Quarter ?? string.Empty, $"%{filter.Quarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.SubQuarter))
        {
            query = query.Where(b => EF.Functions.ILike(b.SubQuarter ?? string.Empty, $"%{filter.SubQuarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatisticalArea))
        {
            query = query.Where(b => EF.Functions.ILike(b.StatisticalArea ?? string.Empty, $"%{filter.StatisticalArea}%"));
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
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (building is null)
        {
            return NotFound();
        }

        var externalData = await _externalDataService.GetBuildingDataAsync(id, cancellationToken);
        var logs = await _context.BuildingLogs
            .Where(l => l.BuildingId == id)
            .Include(l => l.CreatedByUser)
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
                l.CreatedByUser != null ? l.CreatedByUser.Username : null,
                building.StreetName,
                building.HouseNumber,
                building.BuildingName,
                building.Neighborhood,
                building.BldSivug,
                building.ShikumStatus,
                building.StatusSummary,
                building.SugBaalut,
                building.Quarter,
                building.SubQuarter,
                building.StatisticalArea))
            .ToListAsync(cancellationToken);

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
            ParsePhotoUrls(building.PhotoUrls),
            externalData,
            logs,
            fields);

        return Ok(detail);
    }

    [HttpGet("template")]
    [Authorize(Policy = "Viewer")]
    public ActionResult<IEnumerable<BuildingFieldDto>> GetBuildingTemplate()
    {
        var fields = BuildFieldsSnapshot(new Building());
        return Ok(fields);
    }

    [HttpGet("{id:int}/card")]
    [Authorize(Policy = "Viewer")]
    public async Task<IActionResult> ExportBuildingCard(int id, CancellationToken cancellationToken)
    {
        var building = await _context.Buildings
            .Include(b => b.Street)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (building is null)
        {
            return NotFound();
        }

        var templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "Data", "BuildingCardTemplate.pptx");
        if (!System.IO.File.Exists(templatePath))
        {
            return NotFound("Building card template not found.");
        }

        var streetName = ValueOrDash(building.Street?.Name ?? building.StreetName);
        var houseNumber = ValueOrDash(building.HouseNumber);
        var ownershipLabel = building.SugBaalut.HasValue
            ? SelectTables.GetOptions("Tbl_SugBaalut")
                .FirstOrDefault(option => option.Value == building.SugBaalut.Value)
                ?.Label
            : null;
        var sivugLabel = building.BldSivug.HasValue
            ? SelectTables.GetOptions("Tbl_Sivug")
                .FirstOrDefault(option => option.Value == building.BldSivug.Value)
                ?.Label
            : null;
        var yeudLabel = building.Yeud.HasValue
            ? SelectTables.GetOptions("Tbl_YK")
                .FirstOrDefault(option => option.Value == building.Yeud.Value)
                ?.Label
            : null;
        var kidumTichnunLabel = building.KidumTichnunStatus.HasValue
            ? SelectTables.GetOptions("Tbl_KidumTichnun")
                .FirstOrDefault(option => option.Value == building.KidumTichnunStatus.Value)
                ?.Label
            : null;
        var shimurLabel = building.ForShimur.HasValue
            ? SelectTables.GetOptions("Tbl_ForShimur")
                .FirstOrDefault(option => option.Value == building.ForShimur.Value)
                ?.Label
            : null;

        var replacements = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["{STREET_NAME}"] = streetName,
            ["{HOUSE_NUMBER}"] = houseNumber,
            ["{QUARTER}"] = ValueOrDash(building.Quarter),
            ["{GUSH_M}"] = ValueOrDash(building.GushM),
            ["{PARCEL_M}"] = ValueOrDash(building.ParcelM),
            ["{OWNERSHIP_TYPE}"] = ValueOrDash(ownershipLabel),
            ["{OWNER_DETAILS}"] = ValueOrDash(building.OwnerDetails),
            ["{SIVUG}"] = ValueOrDash(sivugLabel),
            ["{BUILT_AREA_SQM}"] = ValueOrDash(building.ShtachBanuySum),
            ["{YEUD}"] = ValueOrDash(yeudLabel),
            ["{KIDUM_TICHNUN}"] = ValueOrDash(kidumTichnunLabel),
            ["{SHIMUR_YEUD}"] = ValueOrDash(shimurLabel),
            ["{OWNER_POSITION}"] = ValueOrDash(building.OwnerPosition),
            ["{MUNI_POSITION}"] = ValueOrDash(building.MiuniPosition),
            ["{PIKUACH_KLALI}"] = FormatTableValue(building.PikuachKlali, 9),
            ["{PIKUACH_AL_BNIYA}"] = FormatTableValue(building.PikuachAlBniya, 9),
            ["{TZAV_SHIPUTZ_FRONTS}"] = FormatTableValue(building.TzavShiputzFronts, 9),
            ["{BUILDING_PERMIT}"] = ResolveIsThere(building.HeterBniya),
            ["{DAMAGE_PERCENT}"] = building.DamagePercentage.HasValue
                ? $"{building.DamagePercentage.Value}%"
                : "-"
        };

        var pptxBytes = BuildCardPptx(templatePath, replacements);
        var fileName = $"building-card-{id}.pptx";

        return File(
            pptxBytes,
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            fileName);
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
            query = query.Where(b => EF.Functions.ILike(b.Quarter ?? string.Empty, $"%{filter.Quarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.SubQuarter))
        {
            query = query.Where(b => EF.Functions.ILike(b.SubQuarter ?? string.Empty, $"%{filter.SubQuarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatisticalArea))
        {
            query = query.Where(b => EF.Functions.ILike(b.StatisticalArea ?? string.Empty, $"%{filter.StatisticalArea}%"));
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
        var groupedFields = OrderFieldGroupsForExport(fieldDefinitions);
        var orderedFields = groupedFields.SelectMany(group => group.Fields).ToList();

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
            for (var col = 0; col < orderedFields.Count; col++)
            {
                var columnName = orderedFields[col].ColumnName;
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
        if (request.Photos is { Length: > 1 })
        {
            return BadRequest("Only one image per building is supported.");
        }

        var houseNumber = request.HouseNumber?.Trim() ?? string.Empty;
        var building = new Building
        {
            FldId = request.FldId,
            HouseNumber = houseNumber,
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

        if (!request.AllowDuplicate)
        {
            var duplicateExists = await _context.Buildings.AnyAsync(
                b => b.StreetCode == building.StreetCode && b.HouseNumber == houseNumber,
                cancellationToken);
            if (duplicateExists)
            {
                return Conflict(new { error = "נמצאה כפילות", isDuplicate = true });
            }
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
        if (request.Photos is { Length: > 1 })
        {
            return BadRequest("Only one image per building is supported.");
        }

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

        if (!request.AllowDuplicate)
        {
            var effectiveStreetCode = building.StreetCode;
            var effectiveHouseNumber = (building.HouseNumber ?? string.Empty).Trim();
            if (effectiveStreetCode.HasValue && !string.IsNullOrWhiteSpace(effectiveHouseNumber))
            {
                var duplicateExists = await _context.Buildings.AnyAsync(
                    b => b.Id != building.Id &&
                         b.StreetCode == effectiveStreetCode &&
                         b.HouseNumber == effectiveHouseNumber,
                    cancellationToken);
                if (duplicateExists)
                {
                    return Conflict(new { error = "נמצאה כפילות", isDuplicate = true });
                }
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
                if (p.Name is nameof(Building.ExternalSnapshots) or nameof(Building.Street))
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
            ["תאריך עדכון תמצית מצב"] = "תאריך שינוי",
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

        if (excelName == "ID" || excelName == "תאריך שינוי")
        {
            return excelName;
        }

        return $"{excelName} ({fieldName})";
    }

    private sealed record OrderedFieldGroup(string Category, List<BuildingFieldDto> Fields);

    private static IReadOnlyList<OrderedFieldGroup> OrderFieldGroupsForExport(IReadOnlyList<BuildingFieldDto> fields)
    {
        if (fields.Count == 0)
        {
            return Array.Empty<OrderedFieldGroup>();
        }

        var indexed = fields
            .Select((field, index) => new { field, index })
            .ToList();

        return indexed
            .GroupBy(entry => string.IsNullOrWhiteSpace(entry.field.Category) ? "כללי" : entry.field.Category)
            .Select(group => new
            {
                Category = group.Key,
                Index = group.Min(entry => entry.index),
                Fields = group.ToList()
            })
            .OrderBy(group => GetCategoryPriority(group.Category))
            .ThenBy(group => group.Index)
            .Select(group => new OrderedFieldGroup(
                group.Category,
                group.Fields
                    .OrderBy(entry => GetFieldPriority(entry.field.FieldName))
                    .ThenBy(entry => entry.index)
                    .Select(entry => entry.field)
                    .ToList()))
            .ToList();
    }

    private static int GetCategoryPriority(string category)
    {
        if (category == "מידע כללי") return 0;
        if (category == "פרטים מזהים") return 1;
        return 2;
    }

    private static int GetFieldPriority(string? fieldName)
    {
        if (fieldName == "סיווג") return 0;
        if (fieldName == "סטטוס שיקום") return 1;
        return 2;
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

        if (property.Name == nameof(Building.PhotoUrls))
        {
            var raw = value as string;
            return string.IsNullOrWhiteSpace(raw) ? null : "קיים";
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

        Guid? actorId = await ResolveActorIdAsync(cancellationToken);
        var fieldsSnapshot = BuildFieldsSnapshot(building);
        var externalData = await _externalDataService.GetBuildingDataAsync(id, cancellationToken);
        var deleteSnapshot = new
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
            Changes = BuildDeleteChanges(fieldsSnapshot),
            Fields = fieldsSnapshot,
            ExternalData = externalData
        };

        _context.BuildingLogs.Add(new BuildingLog
        {
            BuildingId = building.Id,
            Title = "מחיקת מבנה",
            Message = JsonSerializer.Serialize(deleteSnapshot),
            Category = "מחיקה",
            Severity = "warning",
            CreatedByUserId = actorId,
            CreatedAt = IsraelTime.NowUtc
        });

        _context.Buildings.Remove(building);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), id.ToString(), "Delete", new { request.Reason }, cancellationToken);

        return NoContent();
    }

    [HttpPost("restore/{logId:int}")]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult<BuildingSummaryDto>> RestoreBuilding(int logId, CancellationToken cancellationToken)
    {
        var log = await _context.BuildingLogs
            .FirstOrDefaultAsync(l => l.Id == logId, cancellationToken);

        if (log is null)
        {
            return NotFound();
        }

        if (!IsDeleteLog(log))
        {
            return BadRequest("Log entry is not a delete record.");
        }

        var snapshot = DeserializeSnapshot(log.Message);
        if (snapshot is null)
        {
            return BadRequest("Missing snapshot for restore.");
        }

        var buildingId = snapshot.Id != 0 ? snapshot.Id : log.BuildingId;
        var exists = await _context.Buildings.AnyAsync(b => b.Id == buildingId, cancellationToken);
        if (exists)
        {
            return Conflict("Building already exists.");
        }

        var building = new Building
        {
            Id = buildingId,
            FldId = snapshot.FldId,
            BuildingName = snapshot.BuildingName ?? string.Empty,
            StreetName = snapshot.StreetName ?? string.Empty,
            HouseNumber = snapshot.HouseNumber ?? string.Empty,
            Neighborhood = snapshot.Neighborhood ?? string.Empty,
            BldSivug = snapshot.BldSivug,
            ShikumStatus = snapshot.ShikumStatus ?? BuildingStatus.Unknown,
            StatusSummary = snapshot.StatusSummary ?? string.Empty,
            StatusSummaryUpdatedAt = snapshot.StatusSummaryUpdatedAt
        };

        var streetId = snapshot.StreetCode;
        if (!streetId.HasValue && snapshot.Fields is not null)
        {
            var streetField = snapshot.Fields.FirstOrDefault(field =>
                string.Equals(field.ColumnName, "StreetId", StringComparison.OrdinalIgnoreCase));
            if (streetField?.RawValue is not null)
            {
                streetId = streetField.RawValue;
            }
            else if (!string.IsNullOrWhiteSpace(streetField?.Value) &&
                     int.TryParse(streetField.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed))
            {
                streetId = parsed;
            }
        }

        if (!streetId.HasValue)
        {
            return BadRequest("StreetId is required to restore building.");
        }

        if (streetId.Value == NoStreetId)
        {
            building.StreetCode = NoStreetId;
            building.StreetName = NoStreetName;
        }
        else
        {
            var street = await _context.Streets.FirstOrDefaultAsync(
                s => s.StreetId == streetId.Value,
                cancellationToken);
            if (street is null)
            {
                return BadRequest($"Street with id {streetId.Value} not found.");
            }

            building.StreetCode = street.StreetId;
            building.StreetName = street.Name;
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

        if (snapshot.Fields is not null)
        {
            foreach (var field in snapshot.Fields)
            {
                if (string.IsNullOrWhiteSpace(field.ColumnName))
                {
                    continue;
                }

                if (string.Equals(field.ColumnName, "StreetName", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(field.ColumnName, "StreetId", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!propertyByColumn.TryGetValue(field.ColumnName, out var property))
                {
                    continue;
                }

                var raw = field.RawValue?.ToString(CultureInfo.InvariantCulture) ?? field.Value;
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var converted = ConvertFieldValue(raw, property);
                if (converted is InvalidFieldValue invalid)
                {
                    return BadRequest($"Invalid value for '{field.ColumnName}': {invalid.Message}");
                }

                property.SetValue(building, converted);
            }
        }

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync(cancellationToken);

        Guid? actorId = await ResolveActorIdAsync(cancellationToken);
        var restoredFields = BuildFieldsSnapshot(building);
        BuildingExternalDataDto externalData;
        try
        {
            externalData = await _externalDataService.GetBuildingDataAsync(building.Id, cancellationToken);
        }
        catch
        {
            externalData = new BuildingExternalDataDto(
                new ExternalSystemSnapshotDto("GIS", "{}", IsraelTime.NowUtc),
                new ExternalSystemSnapshotDto("Water", "{}", IsraelTime.NowUtc),
                new ExternalSystemSnapshotDto("Electricity", "{}", IsraelTime.NowUtc),
                new ExternalSystemSnapshotDto("Tax", "{}", IsraelTime.NowUtc),
                new ExternalSystemSnapshotDto("CRM106", "{}", IsraelTime.NowUtc));
        }

        var restoreSnapshot = new
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
            Changes = BuildCreateChanges(building),
            Fields = restoredFields,
            ExternalData = externalData
        };

        _context.BuildingLogs.Add(new BuildingLog
        {
            BuildingId = building.Id,
            Title = "שחזור מבנה",
            Message = JsonSerializer.Serialize(restoreSnapshot),
            Category = "יצירה",
            Severity = "info",
            CreatedByUserId = actorId,
            CreatedAt = IsraelTime.NowUtc
        });
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            await _context.Database.ExecuteSqlRawAsync(
                "SELECT setval(pg_get_serial_sequence('\"Buildings\"','\"Id\"'), GREATEST((SELECT MAX(\"Id\") FROM \"Buildings\"), 1))",
                cancellationToken);
        }
        catch
        {
            // best-effort sequence update
        }

        await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "Restore", null, cancellationToken);

        return new BuildingSummaryDto(
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
            building.StatisticalArea);
    }

    private static bool IsDeleteLog(BuildingLog log)
    {
        return string.Equals(log.Category, "מחיקה", StringComparison.OrdinalIgnoreCase) ||
               string.Equals(log.Title, "מחיקת מבנה", StringComparison.OrdinalIgnoreCase);
    }

    private static BuildingSnapshot? DeserializeSnapshot(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<BuildingSnapshot>(
                message,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }

    private static byte[] BuildCardPptx(
        string templatePath,
        IReadOnlyDictionary<string, string> replacements)
    {
        using var templateStream = System.IO.File.OpenRead(templatePath);
        using var templateZip = new ZipArchive(templateStream, ZipArchiveMode.Read);
        using var outputStream = new MemoryStream();
        using (var outputZip = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in templateZip.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    outputZip.CreateEntry(entry.FullName);
                    continue;
                }

                var newEntry = outputZip.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var newEntryStream = newEntry.Open();

                if (string.Equals(entry.FullName, "ppt/slides/slide1.xml", StringComparison.OrdinalIgnoreCase))
                {
                    var doc = XDocument.Load(entryStream);
                    ReplaceText(doc, replacements);
                    doc.Save(newEntryStream, System.Xml.Linq.SaveOptions.DisableFormatting);
                }
                else
                {
                    entryStream.CopyTo(newEntryStream);
                }
            }
        }

        return outputStream.ToArray();
    }

    private static void ReplaceText(XDocument doc, IReadOnlyDictionary<string, string> replacements)
    {
        if (replacements.Count == 0)
        {
            return;
        }

        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        foreach (var textNode in doc.Descendants(a + "t"))
        {
            var value = textNode.Value;
            if (string.IsNullOrEmpty(value))
            {
                continue;
            }

            foreach (var (key, replacement) in replacements)
            {
                value = value.Replace(key, replacement);
            }

            textNode.Value = value;
        }
    }

    private static string ValueOrDash(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? "-" : value.Trim();
    }

    private static string[] ParsePhotoUrls(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            return new[] { trimmed };
        }

        return trimmed.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static string ValueOrDash(int? value)
    {
        return value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : "-";
    }

    private static string ResolveYesNoMaybe(int? value)
    {
        if (!value.HasValue)
        {
            return "-";
        }

        return SelectTables
            .GetOptions("Tbl_Y_N_Maybe")
            .FirstOrDefault(option => option.Value == value.Value)
            ?.Label
            ?? value.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static string ResolveIsThere(int? value)
    {
        if (!value.HasValue)
        {
            return "-";
        }

        return SelectTables
            .GetOptions("Tbl_IsThere")
            .FirstOrDefault(option => option.Value == value.Value)
            ?.Label
            ?? value.Value.ToString(CultureInfo.InvariantCulture);
    }

    private static string FormatTableValue(string? value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "-";
        }

        var normalized = value
            .Replace("\r", " ", StringComparison.Ordinal)
            .Replace("\n", " ", StringComparison.Ordinal)
            .Trim();

        if (normalized.Length <= maxChars || maxChars <= 0)
        {
            return normalized;
        }

        if (maxChars <= 3)
        {
            return normalized[..maxChars];
        }

        return string.Concat(normalized.AsSpan(0, maxChars - 3), "...");
    }

    private static IReadOnlyList<FieldChange> BuildDeleteChanges(IReadOnlyList<BuildingFieldDto> fields)
    {
        return fields
            .Where(field => !string.IsNullOrWhiteSpace(field.ColumnName))
            .Select(field => new FieldChange(
                field.ColumnName,
                field.FieldName,
                string.IsNullOrWhiteSpace(field.Value) ? null : field.Value,
                "-"))
            .ToList();
    }

    private sealed record BuildingSnapshot(
        int Id,
        int? FldId,
        int? StreetCode,
        string? BuildingName,
        string? StreetName,
        string? HouseNumber,
        string? Neighborhood,
        int? BldSivug,
        BuildingStatus? ShikumStatus,
        string? StatusSummary,
        DateTime? StatusSummaryUpdatedAt,
        List<BuildingFieldDto>? Fields);
}
