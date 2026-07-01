using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using ClosedXML.Excel;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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
using InvalidFieldValue = WebServer.Services.BuildingRules.InvalidFieldValue;

namespace WebServer.Controllers;

[Route("api/[controller]")]
public class BuildingsController : ApiControllerBase
{
    private const int NoStreetId = -1;
    private const string NoStreetName = "ללא שם רחוב";
    private const int MaxPhotoSizeBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;
    private readonly IWebHostEnvironment _hostEnvironment;
    private readonly IGisSnapshotService _gisSnapshotService;

    public BuildingsController(
        AppDbContext context,
        IAuditService auditService,
        IWebHostEnvironment hostEnvironment,
        IGisSnapshotService gisSnapshotService)
    {
        _context = context;
        _auditService = auditService;
        _hostEnvironment = hostEnvironment;
        _gisSnapshotService = gisSnapshotService;
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

    [HttpGet("gis-candidates")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<IEnumerable<BuildingGisCandidateDto>>> GetGisCandidates(CancellationToken cancellationToken)
    {
        var buildings = await _context.Buildings
            .Include(b => b.Street)
            .AsNoTracking()
            .OrderBy(b => b.StreetName)
            .ThenBy(b => b.HouseNumber)
            .ToListAsync(cancellationToken);

        return Ok(buildings.Select(b => new BuildingGisCandidateDto(
            b.Id,
            b.BuildingName,
            b.Street?.Name ?? b.StreetName,
            b.HouseNumber,
            b.Neighborhood,
            b.ShikumStatus,
            b.BldSivug,
            BuildGisLocation(b))));
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
            BuildGisLocation(building),
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

        var replacements = BuildCardReplacements(building);
        var (imageBytes, imageExtension) = GetCardImage(building);
        var mapImageBytes = await _gisSnapshotService.CreateBuildingSnapshotAsync(building, cancellationToken);
        var pptxBytes = BuildCardPptx(templatePath, replacements, imageBytes, imageExtension, mapImageBytes);
        var fileName = $"building-card-{id}.pptx";

        return File(
            pptxBytes,
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            fileName);
    }

    public sealed record ExportCardSelectionRequest(List<int> Ids);

    [HttpPost("export-cards")]
    [Authorize(Policy = "Viewer")]
    public async Task<IActionResult> ExportBuildingCards(
        [FromBody] ExportCardSelectionRequest request,
        CancellationToken cancellationToken)
    {
        var ids = request?.Ids ?? new List<int>();

        var templatePath = Path.Combine(_hostEnvironment.ContentRootPath, "Data", "BuildingCardTemplate.pptx");
        if (!System.IO.File.Exists(templatePath))
        {
            return NotFound("Building card template not found.");
        }

        List<Building> buildings = new();
        if (ids.Count > 0)
        {
            buildings = await _context.Buildings
                .Include(b => b.Street)
                .Where(b => ids.Contains(b.Id))
                .ToListAsync(cancellationToken);
        }

        var buildingsById = buildings.ToDictionary(b => b.Id, b => b);
        var orderedBuildings = ids
            .Select(id => buildingsById.TryGetValue(id, out var building) ? building : null)
            .Where(building => building is not null)
            .ToList();

        var payloads = new List<BuildingCardPayload>();
        foreach (var building in orderedBuildings)
        {
            var replacements = BuildCardReplacements(building!);
            var (imageBytes, imageExtension) = GetCardImage(building!);
            var mapImageBytes = await _gisSnapshotService.CreateBuildingSnapshotAsync(building!, cancellationToken);
            payloads.Add(new BuildingCardPayload(replacements, imageBytes, imageExtension, mapImageBytes));
        }

        var pptxBytes = BuildCardsPptx(templatePath, payloads);
        var fileName = $"building-cards-{FileDateStamp()}.pptx";

        return File(
            pptxBytes,
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            fileName);
    }

    [HttpGet("export")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportBuildings(
        [FromQuery] BuildingFilterParameters filter,
        [FromQuery] bool includeImages,
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

        return BuildBuildingsExport(buildings, includeImages);
    }

    private IActionResult BuildBuildingsExport(IReadOnlyList<Building> buildings, bool includeImages)
    {
        var fieldDefinitions = BuildFieldsSnapshot(buildings.FirstOrDefault() ?? new Building(), includePhotos: false);
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
            var snapshot = BuildFieldsSnapshot(building, includePhotos: false);
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
        using var excelStream = new MemoryStream();
        workbook.SaveAs(excelStream);
        excelStream.Position = 0;

        if (!includeImages)
        {
            var fileName = $"buildings-{FileDateStamp()}.xlsx";
            return File(
                excelStream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }

        using var zipStream = new MemoryStream();
        using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            var excelEntry = archive.CreateEntry("buildings.xlsx", CompressionLevel.Optimal);
            using (var entryStream = excelEntry.Open())
            {
                excelStream.Position = 0;
                excelStream.CopyTo(entryStream);
            }

            foreach (var building in buildings)
            {
                var photo = ParsePhotoUrls(building.PhotoUrls).FirstOrDefault();
                if (string.IsNullOrWhiteSpace(photo))
                {
                    continue;
                }

                if (!TryDecodeImageDataUrl(photo, out var bytes, out var extension))
                {
                    continue;
                }

                if (bytes.Length > MaxPhotoSizeBytes)
                {
                    continue;
                }

                var imageEntry = archive.CreateEntry($"images/{building.Id}.{extension}", CompressionLevel.Optimal);
                using var imageStream = imageEntry.Open();
                imageStream.Write(bytes, 0, bytes.Length);
            }
        }

        var zipName = $"buildings-{FileDateStamp()}.zip";
        return File(zipStream.ToArray(), "application/zip", zipName);
    }

    private static string FileDateStamp() => IsraelTime.Convert(IsraelTime.NowUtc).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public sealed record ExportSelectionRequest(List<int> Ids);

    [HttpPost("export")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ExportBuildingsSelection(
        [FromBody] ExportSelectionRequest request,
        [FromQuery] bool includeImages,
        CancellationToken cancellationToken)
    {
        if (request?.Ids is null || request.Ids.Count == 0)
        {
            return BuildBuildingsExport(Array.Empty<Building>(), includeImages);
        }

        var buildings = await _context.Buildings
            .AsNoTracking()
            .Where(b => request.Ids.Contains(b.Id))
            .OrderBy(b => b.StreetName)
            .ThenBy(b => b.HouseNumber)
            .ToListAsync(cancellationToken);

        return BuildBuildingsExport(buildings, includeImages);
    }

    [HttpPost("convert-template")]
    [Authorize(Policy = "Admin")]
    public IActionResult ConvertBuildingsTemplate([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Import file is required.");
        }

        using var stream = file.OpenReadStream();
        var buildings = BuildingsExcelImporter.ReadBuildingsFromStream(stream, out var error);
        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest(error);
        }

        return BuildBuildingsExport(buildings, includeImages: false);
    }

    public sealed class BuildingsImportRequest
    {
        public IFormFile? File { get; set; }
        public bool AllowUpdates { get; set; }
        public bool SkipDuplicates { get; set; }
    }

    public sealed class ImportPreviewRequest
    {
        public IFormFile? File { get; set; }
    }

    public sealed record ImportExistingMatch(
        int Id,
        string StreetName,
        string HouseNumber,
        string BuildingName,
        List<BuildingFieldDto> Fields);

    public sealed record ImportValidationIssue(string ColumnName, string Message);

    public sealed record ImportPreviewRow(
        int RowNumber,
        Dictionary<string, string?> Values,
        List<ImportExistingMatch> AddressMatches,
        ImportExistingMatch? IdMatch,
        bool HasIdConflict,
        bool ExactMatch,
        List<string> MissingRequired,
        List<ImportValidationIssue> InvalidValues,
        List<string> Warnings,
        List<BuildingFieldDto> ImportFields);

    public sealed record ImportPreviewResponse(List<ImportPreviewRow> Rows);

    public sealed record ImportApplyRequest(List<ImportApplyRow> Rows);

    public sealed record ImportValidateRequest(Dictionary<string, string?> Values);

    public sealed record ImportApplyRow(
        int RowNumber,
        string Action,
        Dictionary<string, string?> Values,
        bool AllowDuplicate,
        List<int>? ReplaceIds);

    private sealed record ImportDuplicateInfo(int RowNumber, int StreetId, string HouseNumber, int ExistingId);
    private sealed record ImportApplyResult(string? Error, List<FieldChange> Changes);
    private sealed record ImportRowData(int RowNumber, Dictionary<string, string?> Values, List<string> Warnings);
    private sealed record ImportPackage(
        Stream ExcelStream,
        Dictionary<int, string> ImagesById,
        Dictionary<int, string> ImageWarningsById);
    private sealed record ImportHeaderMap(Dictionary<int, string> Columns, int HeaderRow);

    [HttpPost("import")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ImportBuildings(
        [FromForm] BuildingsImportRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("Import file is required.");
        }

        ImportPackage package;
        try
        {
            package = await ReadImportPackageAsync(request.File, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        using var stream = package.ExcelStream;
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            return BadRequest("Excel worksheet not found.");
        }

        var fieldDefinitions = BuildFieldsSnapshot(new Building(), includePhotos: true);
        var labelToColumn = fieldDefinitions
            .ToDictionary(
                field => GetExcelAwareLabel(field.FieldName),
                field => field.ColumnName,
                StringComparer.OrdinalIgnoreCase);

        var headerMap = BuildImportHeaderMap(worksheet, labelToColumn);
        if (headerMap.Columns.Count == 0)
        {
            return BadRequest("Excel headers do not match the export format.");
        }

        var importRows = new List<(int RowNumber, Dictionary<string, string?> Values, int StreetId, string HouseNumber)>();

        var importData = ReadImportRows(worksheet, headerMap, package.ImagesById, package.ImageWarningsById);

        foreach (var row in importData)
        {
            var values = row.Values;

            values.TryGetValue("StreetId", out var streetIdRaw);
            if (string.IsNullOrWhiteSpace(streetIdRaw) ||
                !int.TryParse(streetIdRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var streetId))
            {
                return BadRequest($"Row {row.RowNumber}: StreetId is required and must be a number.");
            }

            values.TryGetValue("BldNum", out var houseNumberRaw);
            var houseNumber = houseNumberRaw?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(houseNumber))
            {
                return BadRequest($"Row {row.RowNumber}: House number is required.");
            }

            importRows.Add((row.RowNumber, values, streetId, houseNumber));
        }

        if (importRows.Count == 0)
        {
            return BadRequest("No data rows found.");
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

        var duplicateRows = new List<ImportDuplicateInfo>();
        var existingByKey = new Dictionary<(int StreetId, string HouseNumber), Building>();

        foreach (var row in importRows)
        {
            var key = (row.StreetId, row.HouseNumber);
            if (!existingByKey.TryGetValue(key, out var existing))
            {
                existing = await _context.Buildings
                    .Include(b => b.Street)
                    .FirstOrDefaultAsync(
                        b => b.StreetCode == row.StreetId && b.HouseNumber == row.HouseNumber,
                        cancellationToken);
                if (existing != null)
                {
                    existingByKey[key] = existing;
                }
            }

            if (existing != null)
            {
                duplicateRows.Add(new ImportDuplicateInfo(row.RowNumber, row.StreetId, row.HouseNumber, existing.Id));
            }
        }

        if (duplicateRows.Count > 0 && !request.AllowUpdates && !request.SkipDuplicates)
        {
            return Conflict(new
            {
                error = "נמצאה כפילות",
                isDuplicate = true,
                duplicates = duplicateRows.Select(d => new
                {
                    d.RowNumber,
                    d.StreetId,
                    d.HouseNumber,
                    d.ExistingId
                })
            });
        }

        var createdCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;
        var importLogs = new List<(Building Building, List<FieldChange> Changes, bool IsCreate)>();
        var actorId = await ResolveActorIdAsync(cancellationToken);

        foreach (var row in importRows)
        {
            var key = (row.StreetId, row.HouseNumber);
            if (existingByKey.TryGetValue(key, out var existing))
            {
                if (request.SkipDuplicates)
                {
                    skippedCount++;
                    continue;
                }

                var updateResult = await ApplyImportRow(existing, row.Values, row.StreetId, propertyByColumn, cancellationToken);
                if (!string.IsNullOrWhiteSpace(updateResult.Error))
                {
                    return BadRequest($"Row {row.RowNumber}: {updateResult.Error}");
                }
                importLogs.Add((existing, updateResult.Changes, false));
                updatedCount++;
                continue;
            }

            var building = new Building();
            var createResult = await ApplyImportRow(building, row.Values, row.StreetId, propertyByColumn, cancellationToken);
            if (!string.IsNullOrWhiteSpace(createResult.Error))
            {
                return BadRequest($"Row {row.RowNumber}: {createResult.Error}");
            }
            _context.Buildings.Add(building);
            importLogs.Add((building, BuildCreateChanges(building).ToList(), true));
            createdCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (building, changes, isCreate) in importLogs)
        {
            var fieldsSnapshot = BuildFieldsSnapshot(building, includePhotos: true);
            _context.BuildingLogs.Add(new BuildingLog
            {
                BuildingId = building.Id,
                Title = isCreate ? "יצירת מבנה (ייבוא)" : "עדכון מבנה (ייבוא)",
                Message = JsonSerializer.Serialize(new
                {
                    building.Id,
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
                    Fields = fieldsSnapshot
                }),
                Category = isCreate ? "Create" : "Edit",
                Severity = "info",
                CreatedByUserId = actorId,
                CreatedAt = IsraelTime.NowUtc
            });

            await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "Import", changes, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await UpdateBuildingIdSequenceAsync(cancellationToken);

        return Ok(new
        {
            created = createdCount,
            updated = updatedCount,
            skipped = skippedCount
        });
    }

    [HttpPost("import/preview")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ImportPreviewResponse>> ImportBuildingsPreview(
        [FromForm] ImportPreviewRequest request,
        CancellationToken cancellationToken)
    {
        if (request.File == null || request.File.Length == 0)
        {
            return BadRequest("Import file is required.");
        }

        ImportPackage package;
        try
        {
            package = await ReadImportPackageAsync(request.File, cancellationToken);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }

        using var stream = package.ExcelStream;
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            return BadRequest("Excel worksheet not found.");
        }

        var fieldDefinitions = BuildFieldsSnapshot(new Building(), includePhotos: true);
        var labelToColumn = fieldDefinitions
            .ToDictionary(
                field => GetExcelAwareLabel(field.FieldName),
                field => field.ColumnName,
                StringComparer.OrdinalIgnoreCase);

        var headerMap = BuildImportHeaderMap(worksheet, labelToColumn);
        if (headerMap.Columns.Count == 0)
        {
            return BadRequest("Excel headers do not match the export format.");
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

        var importRows = ReadImportRows(worksheet, headerMap, package.ImagesById, package.ImageWarningsById);
        if (importRows.Count == 0)
        {
            return BadRequest("No data rows found.");
        }

        var rehabSivugValue = ResolveRehabSivugValue();

        var normalizedRows = importRows
            .Select(row => (row.RowNumber, Values: NormalizeImportValues(row.Values), row.Warnings))
            .ToList();

        var idCounts = normalizedRows
            .Select(row => TryParsePositiveInt(row.Values.TryGetValue("Id", out var raw) ? raw : null))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());

        var requestedIds = idCounts.Keys.ToList();
        var existingById = requestedIds.Count == 0
            ? new Dictionary<int, Building>()
            : await _context.Buildings
                .Include(b => b.Street)
                .Where(b => requestedIds.Contains(b.Id))
                .ToDictionaryAsync(b => b.Id, cancellationToken);

        var addressStreetIds = normalizedRows
            .Select(row => TryParseStreetId(row.Values.TryGetValue("StreetId", out var raw) ? raw : null))
            .Where(value => value.HasValue)
            .Select(value => value!.Value)
            .Distinct()
            .ToList();

        var streetIdsToValidate = addressStreetIds
            .Where(value => value != NoStreetId)
            .ToList();

        var existingStreetIds = streetIdsToValidate.Count == 0
            ? new HashSet<int>()
            : (await _context.Streets
                    .Where(s => streetIdsToValidate.Contains(s.StreetId))
                    .Select(s => s.StreetId)
                    .ToListAsync(cancellationToken))
                .ToHashSet();

        var existingAddressCandidates = addressStreetIds.Count == 0
            ? new List<Building>()
            : await _context.Buildings
                .Include(b => b.Street)
                .Where(b => b.StreetCode.HasValue && addressStreetIds.Contains(b.StreetCode.Value))
                .ToListAsync(cancellationToken);

        var existingByAddress = existingAddressCandidates
            .GroupBy(b => (StreetId: b.StreetCode!.Value, House: (b.HouseNumber ?? string.Empty).Trim()))
            .ToDictionary(group => group.Key, group => group.ToList());

        var previewRows = new List<ImportPreviewRow>();

        foreach (var row in normalizedRows)
        {
            var warnings = new List<string>(row.Warnings);
            var values = row.Values;
            var missingRequired = GetMissingRequiredColumns(values, rehabSivugValue, warnings, requireId: false);
            var invalidValues = BuildingRules
                .GetInvalidFieldValues(values, propertyByColumn)
                .Select(issue => new ImportValidationIssue(issue.ColumnName, issue.Message))
                .ToList();

            values.TryGetValue("Id", out var idRaw);
            var idValue = TryParsePositiveInt(idRaw);
            var streetId = TryParseStreetId(values.TryGetValue("StreetId", out var streetRaw) ? streetRaw : null);
            var houseNumber = values.TryGetValue("BldNum", out var houseRaw) ? houseRaw?.Trim() ?? string.Empty : string.Empty;

            var addressMatches = new List<ImportExistingMatch>();
            if (streetId.HasValue && !string.IsNullOrWhiteSpace(houseNumber))
            {
                var key = (StreetId: streetId.Value, House: houseNumber);
                if (existingByAddress.TryGetValue(key, out var matches))
                {
                    addressMatches.AddRange(matches.Select(match => BuildExistingMatch(match, fieldDefinitions)));
                }
            }

            if (streetId.HasValue && streetId.Value != NoStreetId && !existingStreetIds.Contains(streetId.Value))
            {
                invalidValues.Add(new ImportValidationIssue("StreetId", "רחוב לא קיים במערכת."));
            }

            ImportExistingMatch? idMatch = null;
            if (idValue.HasValue && existingById.TryGetValue(idValue.Value, out var existingIdMatch))
            {
                idMatch = BuildExistingMatch(existingIdMatch, fieldDefinitions);
            }

            var hasIdConflict = idMatch != null;
            if (idValue.HasValue && idCounts.TryGetValue(idValue.Value, out var count) && count > 1)
            {
                hasIdConflict = true;
                warnings.Add("ID מופיע יותר מפעם אחת בקובץ הייבוא.");
            }

            var importFields = BuildImportFields(fieldDefinitions, values);
            var exactMatch = false;
            if (idMatch != null && IsExactMatch(fieldDefinitions, values, idMatch))
            {
                exactMatch = true;
            }
            else if (addressMatches.Count > 0 && addressMatches.Any(match => IsExactMatch(fieldDefinitions, values, match)))
            {
                exactMatch = true;
            }

            previewRows.Add(new ImportPreviewRow(
                row.RowNumber,
                values,
                addressMatches,
                idMatch,
                hasIdConflict,
                exactMatch,
                missingRequired,
                invalidValues,
                warnings,
                importFields));
        }

        return Ok(new ImportPreviewResponse(previewRows));
    }

    [HttpPost("import/validate")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<ImportPreviewRow>> ValidateImportRow(
        [FromBody] ImportValidateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Values is null || request.Values.Count == 0)
        {
            return BadRequest("No values supplied.");
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

        var fieldDefinitions = BuildFieldsSnapshot(new Building(), includePhotos: true);
        var warnings = new List<string>();
        var values = NormalizeImportValues(request.Values);
        var rehabSivugValue = ResolveRehabSivugValue();
        var missingRequired = GetMissingRequiredColumns(values, rehabSivugValue, warnings, requireId: false);
        var invalidValues = BuildingRules
            .GetInvalidFieldValues(values, propertyByColumn)
            .Select(issue => new ImportValidationIssue(issue.ColumnName, issue.Message))
            .ToList();

        values.TryGetValue("Id", out var idRaw);
        var idValue = TryParsePositiveInt(idRaw);
        ImportExistingMatch? idMatch = null;
        if (idValue.HasValue)
        {
            var existing = await _context.Buildings
                .Include(b => b.Street)
                .FirstOrDefaultAsync(b => b.Id == idValue.Value, cancellationToken);
            if (existing != null)
            {
                idMatch = BuildExistingMatch(existing, fieldDefinitions);
            }
        }

        var addressMatches = new List<ImportExistingMatch>();
        var streetId = TryParseStreetId(values.TryGetValue("StreetId", out var streetRaw) ? streetRaw : null);
        var houseNumber = values.TryGetValue("BldNum", out var houseRaw) ? houseRaw?.Trim() ?? string.Empty : string.Empty;
        if (streetId.HasValue && !string.IsNullOrWhiteSpace(houseNumber))
        {
            var matches = await _context.Buildings
                .Include(b => b.Street)
                .Where(b => b.StreetCode == streetId.Value && b.HouseNumber == houseNumber)
                .ToListAsync(cancellationToken);
            addressMatches.AddRange(matches.Select(match => BuildExistingMatch(match, fieldDefinitions)));
        }

        if (streetId.HasValue && streetId.Value != NoStreetId)
        {
            var streetExists = await _context.Streets.AnyAsync(
                s => s.StreetId == streetId.Value,
                cancellationToken);
            if (!streetExists)
            {
                invalidValues.Add(new ImportValidationIssue("StreetId", "רחוב לא קיים במערכת."));
            }
        }

        var hasIdConflict = idMatch != null;
        var importFields = BuildImportFields(fieldDefinitions, values);
        var exactMatch = false;
        if (idMatch != null && IsExactMatch(fieldDefinitions, values, idMatch))
        {
            exactMatch = true;
        }
        else if (addressMatches.Count > 0 && addressMatches.Any(match => IsExactMatch(fieldDefinitions, values, match)))
        {
            exactMatch = true;
        }

        return Ok(new ImportPreviewRow(
            0,
            values,
            addressMatches,
            idMatch,
            hasIdConflict,
            exactMatch,
            missingRequired,
            invalidValues,
            warnings,
            importFields));
    }

    [HttpPost("import/apply")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ImportBuildingsApply(
        [FromBody] ImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rows is null || request.Rows.Count == 0)
        {
            return BadRequest("No import rows supplied.");
        }

        var duplicateManualIds = request.Rows
            .Where(row => !string.Equals(row.Action?.Trim(), "skip", StringComparison.OrdinalIgnoreCase))
            .Select(row =>
            {
                string? idRaw = null;
                if (row.Values != null)
                {
                    row.Values.TryGetValue("Id", out idRaw);
                }
                return new
                {
                    row.RowNumber,
                    Id = TryParsePositiveInt(idRaw)
                };
            })
            .Where(entry => entry.Id.HasValue)
            .GroupBy(entry => entry.Id!.Value)
            .Where(group => group.Count() > 1)
            .ToList();

        if (duplicateManualIds.Count > 0)
        {
            return Conflict(new
            {
                error = "ID מופיע יותר מפעם אחת בקובץ הייבוא.",
                isIdDuplicate = true,
                duplicates = duplicateManualIds.Select(group => new
                {
                    Id = group.Key,
                    Rows = group.Select(entry => entry.RowNumber).ToList()
                })
            });
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

        var rehabSivugValue = ResolveRehabSivugValue();
        var createdCount = 0;
        var updatedCount = 0;
        var skippedCount = 0;
        var importLogs = new List<(Building Building, List<FieldChange> Changes, bool IsCreate)>();
        var actorId = await ResolveActorIdAsync(cancellationToken);

        foreach (var row in request.Rows)
        {
            var action = row.Action?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(action))
            {
                return BadRequest($"Row {row.RowNumber}: Action is required.");
            }

            if (action == "skip")
            {
                skippedCount++;
                continue;
            }

            if (action != "create" && action != "replace" && action != "add_anyway")
            {
                return BadRequest($"Row {row.RowNumber}: Unsupported action '{row.Action}'.");
            }

            var values = NormalizeImportValues(row.Values ?? new Dictionary<string, string?>());
            var warnings = new List<string>();
            var missingRequired = GetMissingRequiredColumns(values, rehabSivugValue, warnings, requireId: false);
            if (missingRequired.Count > 0)
            {
                return BadRequest($"Row {row.RowNumber}: Missing required fields.");
            }
            var invalidValues = BuildingRules.GetInvalidFieldValues(values, propertyByColumn);
            if (invalidValues.Count > 0)
            {
                return BadRequest(new
                {
                    error = "ערכים לא חוקיים בשורת הייבוא.",
                    row.RowNumber,
                    invalidValues = invalidValues.Select(issue => new ImportValidationIssue(issue.ColumnName, issue.Message)).ToList()
                });
            }

            var streetId = TryParseStreetId(values.TryGetValue("StreetId", out var streetRaw) ? streetRaw : null);
            if (!streetId.HasValue)
            {
                return BadRequest($"Row {row.RowNumber}: StreetId is required and must be a number.");
            }

            var houseNumber = values.TryGetValue("BldNum", out var houseRaw) ? houseRaw?.Trim() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(houseNumber))
            {
                return BadRequest($"Row {row.RowNumber}: House number is required.");
            }

            values.TryGetValue("Id", out var idRaw);
            var manualId = TryParsePositiveInt(idRaw);
            var allowDuplicate = row.AllowDuplicate;

            if (action == "replace")
            {
                var replaceIds = row.ReplaceIds?.Distinct().ToList() ?? new List<int>();
                if (replaceIds.Count == 0)
                {
                    return BadRequest($"Row {row.RowNumber}: Replace requires at least one existing building.");
                }

                var existingToDelete = await _context.Buildings
                    .Where(b => replaceIds.Contains(b.Id))
                    .ToListAsync(cancellationToken);
                if (existingToDelete.Count == 0)
                {
                    return BadRequest($"Row {row.RowNumber}: Existing building not found for replace.");
                }

                if (manualId.HasValue)
                {
                    var idExists = await _context.Buildings.AnyAsync(
                        b => b.Id == manualId.Value && !replaceIds.Contains(b.Id),
                        cancellationToken);
                    if (idExists)
                    {
                        return Conflict(new { error = "קיים מבנה עם ID זה", row.RowNumber, isIdDuplicate = true });
                    }
                }

                foreach (var existing in existingToDelete)
                {
                    var deleteSnapshot = BuildFieldsSnapshot(existing, includePhotos: true);
                    _context.BuildingLogs.Add(new BuildingLog
                    {
                        BuildingId = existing.Id,
                        Title = "מחיקת מבנה (ייבוא)",
                        Message = JsonSerializer.Serialize(new
                        {
                            existing.Id,
                            existing.StreetCode,
                            existing.BuildingName,
                            existing.StreetName,
                            existing.HouseNumber,
                            existing.Neighborhood,
                            existing.BldSivug,
                            existing.ShikumStatus,
                            existing.StatusSummary,
                            existing.StatusSummaryUpdatedAt,
                            Changes = BuildDeleteChanges(deleteSnapshot),
                            Fields = deleteSnapshot
                        }),
                        Category = "מחיקה",
                        Severity = "warning",
                        CreatedByUserId = actorId,
                        CreatedAt = IsraelTime.NowUtc
                    });

                    _context.Buildings.Remove(existing);
                }

                var replacement = new Building();
                if (manualId.HasValue)
                {
                    replacement.Id = manualId.Value;
                }

                var createResult = await ApplyImportRow(replacement, values, streetId.Value, propertyByColumn, cancellationToken);
                if (!string.IsNullOrWhiteSpace(createResult.Error))
                {
                    return BadRequest($"Row {row.RowNumber}: {createResult.Error}");
                }

                _context.Buildings.Add(replacement);
                importLogs.Add((replacement, BuildCreateChanges(replacement).ToList(), true));
                updatedCount++;
                continue;
            }

            if (manualId.HasValue)
            {
                var idExists = await _context.Buildings.AnyAsync(
                    b => b.Id == manualId.Value,
                    cancellationToken);
                if (idExists)
                {
                    return Conflict(new { error = "קיים מבנה עם ID זה", row.RowNumber, isIdDuplicate = true });
                }
            }

            if (!allowDuplicate)
            {
                var duplicateExists = await _context.Buildings.AnyAsync(
                    b => b.StreetCode == streetId.Value && b.HouseNumber == houseNumber,
                    cancellationToken);
                if (duplicateExists)
                {
                    return Conflict(new { error = "נמצאה כפילות", row.RowNumber, isDuplicate = true });
                }
            }

            var building = new Building();
            if (manualId.HasValue)
            {
                building.Id = manualId.Value;
            }

            var result = await ApplyImportRow(building, values, streetId.Value, propertyByColumn, cancellationToken);
            if (!string.IsNullOrWhiteSpace(result.Error))
            {
                return BadRequest($"Row {row.RowNumber}: {result.Error}");
            }

            _context.Buildings.Add(building);
            importLogs.Add((building, BuildCreateChanges(building).ToList(), true));
            createdCount++;
        }

        await _context.SaveChangesAsync(cancellationToken);

        foreach (var (building, changes, isCreate) in importLogs)
        {
            var fieldsSnapshot = BuildFieldsSnapshot(building, includePhotos: true);
            _context.BuildingLogs.Add(new BuildingLog
            {
                BuildingId = building.Id,
                Title = isCreate ? "יצירת מבנה (ייבוא)" : "עדכון מבנה (ייבוא)",
                Message = JsonSerializer.Serialize(new
                {
                    building.Id,
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
                    Fields = fieldsSnapshot
                }),
                Category = isCreate ? "Create" : "Edit",
                Severity = "info",
                CreatedByUserId = actorId,
                CreatedAt = IsraelTime.NowUtc
            });

            await _auditService.RecordAsync(
                CurrentUserId,
                nameof(Building),
                building.Id.ToString(),
                isCreate ? "ImportCreate" : "ImportUpdate",
                changes,
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
        await UpdateBuildingIdSequenceAsync(cancellationToken);

        return Ok(new
        {
            created = createdCount,
            updated = updatedCount,
            skipped = skippedCount
        });
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
        var requiredWarnings = new List<string>();
        var requiredValues = new Dictionary<string, string?>
        {
            ["Id"] = request.Id > 0 ? request.Id.ToString(CultureInfo.InvariantCulture) : null,
            ["StreetId"] = request.StreetId.ToString(CultureInfo.InvariantCulture),
            ["BldNum"] = houseNumber,
            ["BldName"] = request.BuildingName,
            ["BldSivug"] = request.BldSivug?.ToString(CultureInfo.InvariantCulture),
            ["ShikumStatus"] = request.ShikumStatus?.ToString()
        };
        var rehabSivugValue = ResolveRehabSivugValue();
        var missingRequired = GetMissingRequiredColumns(requiredValues, rehabSivugValue, requiredWarnings, requireId: false);
        if (missingRequired.Count > 0)
        {
            return BadRequest(new
            {
                error = "שדות חובה חסרים.",
                missingRequired,
                warnings = requiredWarnings
            });
        }
        if (request.Id > 0)
        {
            var idExists = await _context.Buildings.AnyAsync(b => b.Id == request.Id, cancellationToken);
            if (idExists)
            {
                return Conflict(new { error = "קיים מבנה עם ID זה", isIdDuplicate = true });
            }
        }

        var building = new Building
        {
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
        if (request.Id > 0)
        {
            building.Id = request.Id;
        }

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

        if (request.Id <= 0)
        {
            await UpdateBuildingIdSequenceAsync(cancellationToken);
        }

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync(cancellationToken);

        Guid? actorId = await ResolveActorIdAsync(cancellationToken);

        var fieldsSnapshot = BuildFieldsSnapshot(building, includePhotos: true);
        var createChanges = BuildCreateChanges(building);
        var createSnapshot = new
        {
            building.Id,
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
            Fields = fieldsSnapshot
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
        await UpdateBuildingIdSequenceAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "Create", request, cancellationToken);

        return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, new BuildingSummaryDto(
            building.Id,
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

        if (request.Id <= 0)
        {
            return BadRequest("ID is required.");
        }

        var originalId = building.Id;
        int? pendingIdChange = null;
        if (request.Id != building.Id)
        {
            var idExists = await _context.Buildings.AnyAsync(
                b => b.Id == request.Id && b.Id != building.Id,
                cancellationToken);
            if (idExists)
            {
                return Conflict(new { error = "קיים מבנה עם ID זה", isIdDuplicate = true });
            }

            pendingIdChange = request.Id;
        }

        var finalHouseNumber = request.HouseNumber?.Trim() ?? string.Empty;
        var finalBuildingName = string.IsNullOrWhiteSpace(request.BuildingName) ? building.BuildingName : request.BuildingName;
        var finalSivug = request.BldSivug ?? building.BldSivug;
        var finalShikumStatus = request.ShikumStatus?.ToString() ?? ((int)building.ShikumStatus).ToString(CultureInfo.InvariantCulture);
        var requiredWarnings = new List<string>();
        var requiredValues = new Dictionary<string, string?>
        {
            ["Id"] = request.Id.ToString(CultureInfo.InvariantCulture),
            ["StreetId"] = request.StreetId.ToString(CultureInfo.InvariantCulture),
            ["BldNum"] = finalHouseNumber,
            ["BldName"] = finalBuildingName,
            ["BldSivug"] = finalSivug?.ToString(CultureInfo.InvariantCulture),
            ["ShikumStatus"] = finalShikumStatus
        };
        var rehabSivugValue = ResolveRehabSivugValue();
        var missingRequired = GetMissingRequiredColumns(requiredValues, rehabSivugValue, requiredWarnings, requireId: true);
        if (missingRequired.Count > 0)
        {
            return BadRequest(new
            {
                error = "שדות חובה חסרים.",
                missingRequired,
                warnings = requiredWarnings
            });
        }

        var oldStreetName = building.StreetName;
        var oldHouseNumber = building.HouseNumber;
        var oldBuildingName = building.BuildingName;
        var oldBldSivug = building.BldSivug;
        var oldShikumStatus = building.ShikumStatus;
        var oldStatusSummary = building.StatusSummary;
        var oldStatusSummaryUpdatedAt = building.StatusSummaryUpdatedAt;

        building.HouseNumber = finalHouseNumber;
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

        if (pendingIdChange.HasValue && pendingIdChange.Value != building.Id)
        {
            building = await ReplaceBuildingIdAsync(building, pendingIdChange.Value, cancellationToken);
        }

        var fieldsSnapshot = BuildFieldsSnapshot(building, includePhotos: true);
        var changes = BuildCoreChanges(
            oldStreetName,
            oldHouseNumber,
            oldBuildingName,
            oldBldSivug,
            oldShikumStatus,
            oldStatusSummary,
            oldStatusSummaryUpdatedAt,
            building).ToList();

        if (pendingIdChange.HasValue && pendingIdChange.Value != originalId)
        {
            var idProperty = typeof(Building).GetProperty(nameof(Building.Id));
            if (idProperty is not null)
            {
                var idChange = BuildChange(idProperty, originalId, pendingIdChange.Value);
                if (idChange is not null)
                {
                    changes.Add(idChange);
                }
            }
        }

        var changeSnapshot = new
        {
            building.Id,
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
            Fields = fieldsSnapshot
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

        var originalId = building.Id;
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
        bool houseNumberProvided = false;
        int? desiredStreetId = null;
        bool idProvided = false;
        int? desiredId = null;

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

            if (string.Equals(columnName, "Id", StringComparison.OrdinalIgnoreCase))
            {
                idProvided = true;
                if (string.IsNullOrWhiteSpace(rawValue))
                {
                    desiredId = null;
                }
                else if (int.TryParse(rawValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedId))
                {
                    desiredId = parsedId;
                }
                else
                {
                    return BadRequest("ID must be an integer.");
                }

                continue;
            }

            if (!propertyByColumn.TryGetValue(columnName, out var property))
            {
                continue;
            }

            if (string.Equals(columnName, "BldNum", StringComparison.OrdinalIgnoreCase))
            {
                houseNumberProvided = true;
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

        if (idProvided)
        {
            if (!desiredId.HasValue || desiredId.Value <= 0)
            {
                return BadRequest("ID is required.");
            }

            if (desiredId.Value != building.Id)
            {
                var idExists = await _context.Buildings.AnyAsync(
                    b => b.Id == desiredId.Value && b.Id != building.Id,
                    cancellationToken);
                if (idExists)
                {
                    return Conflict(new { error = "קיים מבנה עם ID זה", isIdDuplicate = true });
                }
            }
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

        if (!request.AllowDuplicate && (streetIdProvided || houseNumberProvided))
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

        var requiredWarnings = new List<string>();
        var requiredValues = new Dictionary<string, string?>
        {
            ["Id"] = building.Id.ToString(CultureInfo.InvariantCulture),
            ["StreetId"] = building.StreetCode?.ToString(CultureInfo.InvariantCulture),
            ["BldNum"] = building.HouseNumber,
            ["BldName"] = building.BuildingName,
            ["BldSivug"] = building.BldSivug?.ToString(CultureInfo.InvariantCulture),
            ["ShikumStatus"] = ((int)building.ShikumStatus).ToString(CultureInfo.InvariantCulture)
        };
        var rehabSivugValue = ResolveRehabSivugValue();
        var missingRequired = GetMissingRequiredColumns(requiredValues, rehabSivugValue, requiredWarnings, requireId: true);
        if (missingRequired.Count > 0)
        {
            return BadRequest(new
            {
                error = "שדות חובה חסרים.",
                missingRequired,
                warnings = requiredWarnings
            });
        }

        var hasChanges = changes.Count > 0 ||
            !string.Equals(originalStreetName, building.StreetName, StringComparison.Ordinal);
        if (hasChanges)
        {
            building.StatusSummaryUpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (idProvided && desiredId.HasValue && desiredId.Value != building.Id)
        {
            building = await ReplaceBuildingIdAsync(building, desiredId.Value, cancellationToken);
        }

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

        if (idProvided && desiredId.HasValue && desiredId.Value != originalId)
        {
            var idProperty = typeof(Building).GetProperty(nameof(Building.Id));
            if (idProperty is not null)
            {
                var idChange = BuildChange(idProperty, originalId, desiredId.Value);
                if (idChange is not null)
                {
                    changes.Add(idChange);
                }
            }
        }

        var fieldsSnapshot = BuildFieldsSnapshot(building, includePhotos: true);
        Guid? actorId = await ResolveActorIdAsync(cancellationToken);
        _context.BuildingLogs.Add(new BuildingLog
        {
            BuildingId = building.Id,
            Title = "עדכון שדות",
            Message = JsonSerializer.Serialize(new
            {
                building.Id,
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
                Fields = fieldsSnapshot
            }),
            Category = "Edit",
            Severity = "info",
            CreatedByUserId = actorId,
            CreatedAt = IsraelTime.NowUtc
        });
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "UpdateFields", request, cancellationToken);

        return await GetBuilding(building.Id, cancellationToken);
    }


    private sealed record FieldChange(string ColumnName, string FieldName, string? OldValue, string? NewValue);

    private static BuildingGisLocationDto BuildGisLocation(Building building)
    {
        return new BuildingGisLocationDto(
            building.Latitude,
            building.Longitude,
            building.GushM,
            building.ParcelM,
            building.GushS,
            building.ParcelS,
            building.Street?.Name ?? building.StreetName,
            building.HouseNumber);
    }

    private static IReadOnlyList<BuildingFieldDto> BuildFieldsSnapshot(Building building, bool includePhotos = false)
    {
        return typeof(Building)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanRead)
            .Where(p =>
            {
                if (p.Name is nameof(Building.Street))
                {
                    return false;
                }

                if (p.Name is nameof(Building.Neighborhood) or nameof(Building.FldId) ||
                    (!includePhotos && p.Name is nameof(Building.PhotoUrls)))
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
                    value = FormatDateTimeValue(dt, p.Name == nameof(Building.StatusSummaryUpdatedAt));
                }
                else if (raw is DateTimeOffset dto)
                {
                    value = FormatDateTimeOffsetValue(dto, includeTime: true);
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
        if (fieldName == "שם רחוב") return 0;
        if (fieldName == "מספר בית") return 1;
        if (fieldName == "כינוי הבניין") return 2;
        if (fieldName == "סיווג") return 3;
        if (fieldName == "סטטוס שיקום") return 4;
        return 5;
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
        AddChangeIfSet(changes, typeof(Building).GetProperty(nameof(Building.PhotoUrls)), null, building.PhotoUrls);
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
            var urls = ParsePhotoUrls(raw);
            return urls.Length > 0 ? urls[0] : null;
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
            return FormatDateTimeValue(dt, property.Name == nameof(Building.StatusSummaryUpdatedAt));
        }

        if (value is DateTimeOffset dto)
        {
            return FormatDateTimeOffsetValue(dto, includeTime: true);
        }

        return value.ToString();
    }

    private static string FormatDateTimeValue(DateTime value, bool includeTime)
    {
        var converted = IsraelTime.Convert((DateTime?)value);
        var format = includeTime ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd";
        return converted?.ToString(format, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string FormatDateTimeOffsetValue(DateTimeOffset value, bool includeTime)
    {
        var format = includeTime ? "yyyy-MM-dd HH:mm" : "yyyy-MM-dd";
        return IsraelTime.Convert(value).ToString(format, CultureInfo.InvariantCulture);
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
        return BuildingRules.ConvertFieldValue(raw, property);
    }

    private async Task<ImportApplyResult> ApplyImportRow(
        Building building,
        IReadOnlyDictionary<string, string?> values,
        int streetId,
        IReadOnlyDictionary<string, PropertyInfo> propertyByColumn,
        CancellationToken cancellationToken)
    {
        var changes = new List<FieldChange>();
        var originalStreetName = building.StreetName;

        foreach (var (columnName, rawValue) in values)
        {
            if (string.IsNullOrWhiteSpace(columnName))
            {
                continue;
            }

            if (string.Equals(columnName, "StreetName", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(columnName, "StreetId", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(columnName, "Id", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!propertyByColumn.TryGetValue(columnName, out var property))
            {
                continue;
            }

            if (string.Equals(columnName, nameof(Building.PhotoUrls), StringComparison.OrdinalIgnoreCase) &&
                !string.IsNullOrWhiteSpace(rawValue) &&
                TryGetPhotoSizeBytes(rawValue, out var photoSizeBytes) &&
                photoSizeBytes > MaxPhotoSizeBytes)
            {
                continue;
            }

            var oldValue = property.GetValue(building);
            var converted = ConvertFieldValue(rawValue, property);
            if (converted is InvalidFieldValue invalid)
            {
                return new ImportApplyResult($"Invalid value for '{columnName}': {invalid.Message}", changes);
            }

            var change = BuildChange(property, oldValue, converted);
            if (change is not null)
            {
                changes.Add(change);
            }
            property.SetValue(building, converted);
        }

        if (streetId == NoStreetId)
        {
            building.StreetCode = NoStreetId;
            building.StreetName = NoStreetName;
        }
        else
        {
            var street = await _context.Streets.FirstOrDefaultAsync(
                s => s.StreetId == streetId,
                cancellationToken);
            if (street == null)
            {
                return new ImportApplyResult($"Street with id {streetId} not found.", changes);
            }

            building.StreetCode = street.StreetId;
            building.StreetName = street.Name;
        }

        if (!string.Equals(originalStreetName, building.StreetName, StringComparison.Ordinal))
        {
            var streetNameProperty = typeof(Building).GetProperty(nameof(Building.StreetName));
            if (streetNameProperty is not null)
            {
                var change = BuildChange(streetNameProperty, originalStreetName, building.StreetName);
                if (change is not null)
                {
                    changes.Add(change);
                }
            }
        }

        building.HouseNumber = (building.HouseNumber ?? string.Empty).Trim();
        building.BuildingName = (building.BuildingName ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(building.BuildingName))
        {
            building.BuildingName = "מבנה";
        }

        if (!building.StatusSummaryUpdatedAt.HasValue)
        {
            building.StatusSummaryUpdatedAt = DateTime.UtcNow;
        }

        return new ImportApplyResult(null, changes);
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

    private async Task UpdateBuildingIdSequenceAsync(CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT setval(
                pg_get_serial_sequence('"Buildings"', 'Id'),
                GREATEST(COALESCE((SELECT MAX("Id") FROM "Buildings"), 1), 1),
                true
            );
            """;
        await _context.Database.ExecuteSqlRawAsync(sql, cancellationToken);
    }

    private static Building CloneBuildingForIdChange(Building source)
    {
        var clone = new Building();
        foreach (var property in typeof(Building).GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!property.CanRead || !property.CanWrite)
            {
                continue;
            }

            if (property.Name is nameof(Building.Id) or nameof(Building.Street))
            {
                continue;
            }

            property.SetValue(clone, property.GetValue(source));
        }

        return clone;
    }

    private async Task<Building> ReplaceBuildingIdAsync(
        Building building,
        int newId,
        CancellationToken cancellationToken)
    {
        var oldId = building.Id;
        var replacement = CloneBuildingForIdChange(building);
        replacement.Id = newId;

        _context.Buildings.Add(replacement);
        await _context.SaveChangesAsync(cancellationToken);

        await _context.BuildingLogs
            .Where(log => log.BuildingId == oldId)
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(log => log.BuildingId, newId),
                cancellationToken);

        await _context.AuditEntries
            .Where(entry => entry.EntityType == nameof(Building) && entry.EntityId == oldId.ToString())
            .ExecuteUpdateAsync(
                updates => updates.SetProperty(entry => entry.EntityId, newId.ToString()),
                cancellationToken);

        _context.Buildings.Remove(building);
        await _context.SaveChangesAsync(cancellationToken);
        await UpdateBuildingIdSequenceAsync(cancellationToken);

        return replacement;
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
        var fieldsSnapshot = BuildFieldsSnapshot(building, includePhotos: true);
        var deleteSnapshot = new
        {
            building.Id,
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
            Fields = fieldsSnapshot
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
        var restoredFields = BuildFieldsSnapshot(building, includePhotos: true);
        var restoreSnapshot = new
        {
            building.Id,
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
            Fields = restoredFields
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

    private sealed record BuildingCardPayload(
        IReadOnlyDictionary<string, string> Replacements,
        byte[]? ImageBytes,
        string? ImageExtension,
        byte[]? MapImageBytes);

    private static Dictionary<string, string> BuildCardReplacements(Building building)
    {
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

        return new Dictionary<string, string>(StringComparer.Ordinal)
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
    }

    private static (byte[]? ImageBytes, string? ImageExtension) GetCardImage(Building building)
    {
        var cardImage = ParsePhotoUrls(building.PhotoUrls).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(cardImage))
        {
            return (null, null);
        }

        if (!TryDecodeImageDataUrl(cardImage, out var decodedBytes, out var decodedExtension))
        {
            return (null, null);
        }

        var imageExtension = NormalizeCardImageExtension(decodedExtension);
        if (imageExtension is null)
        {
            return (null, null);
        }

        return (decodedBytes, imageExtension);
    }

    private static byte[] BuildCardPptx(
        string templatePath,
        IReadOnlyDictionary<string, string> replacements,
        byte[]? imageBytes,
        string? imageExtension,
        byte[]? mapImageBytes)
    {
        const string imageRelId = "rId3";
        const string templateImagePrefix = "ppt/media/image3.";
        const string secondaryImageRelId = "rId2";
        const string secondaryImagePath = "ppt/media/image2.png";
        const string secondaryImageMarkerName = "אליפסה 31";
        var normalizedImageExtension = NormalizeCardImageExtension(imageExtension);
        var hasImage = imageBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(normalizedImageExtension);
        var hasMapImage = mapImageBytes is { Length: > 0 };
        var targetImagePath = normalizedImageExtension is null
            ? null
            : $"ppt/media/image3.{normalizedImageExtension}";
        var targetRelPath = normalizedImageExtension is null
            ? null
            : $"../media/image3.{normalizedImageExtension}";

        using var templateStream = System.IO.File.OpenRead(templatePath);
        using var templateZip = new ZipArchive(templateStream, ZipArchiveMode.Read);
        XDocument? slideDoc = null;
        var slideEntry = templateZip.GetEntry("ppt/slides/slide1.xml");
        var templateImageEntry = templateZip.Entries.FirstOrDefault(entry =>
            entry.FullName.StartsWith(templateImagePrefix, StringComparison.OrdinalIgnoreCase));
        var templateImagePath = templateImageEntry?.FullName;
        if (slideEntry is not null)
        {
            using var slideStream = slideEntry.Open();
            slideDoc = XDocument.Load(slideStream);
        }
        using var outputStream = new MemoryStream();
        using (var outputZip = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in templateZip.Entries)
            {
                if (!string.IsNullOrWhiteSpace(templateImagePath) &&
                    string.Equals(entry.FullName, templateImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasImage || targetImagePath is null || imageBytes is null || normalizedImageExtension is null)
                    {
                        continue;
                    }

                    byte[] outputImageBytes;
                    try
                    {
                        var templateImageBytes = ReadAllBytes(entry.Open());
                        outputImageBytes = BuildLetterboxedImage(
                            imageBytes,
                            templateImageBytes,
                            normalizedImageExtension);
                    }
                    catch
                    {
                        outputImageBytes = imageBytes;
                    }

                    var imageEntry = outputZip.CreateEntry(targetImagePath, CompressionLevel.Optimal);
                    using var imageEntryStream = imageEntry.Open();
                    imageEntryStream.Write(outputImageBytes, 0, outputImageBytes.Length);
                    continue;
                }

                if (string.Equals(entry.FullName, secondaryImagePath, StringComparison.OrdinalIgnoreCase))
                {
                    if (!hasMapImage || mapImageBytes is null)
                    {
                        continue;
                    }

                    byte[] outputMapBytes;
                    try
                    {
                        var templateMapBytes = ReadAllBytes(entry.Open());
                        outputMapBytes = BuildLetterboxedImage(mapImageBytes, templateMapBytes, "png");
                    }
                    catch
                    {
                        outputMapBytes = mapImageBytes;
                    }

                    var mapEntry = outputZip.CreateEntry(secondaryImagePath, CompressionLevel.Optimal);
                    using var mapEntryStream = mapEntry.Open();
                    mapEntryStream.Write(outputMapBytes, 0, outputMapBytes.Length);
                    continue;
                }

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
                    var doc = slideDoc ?? XDocument.Load(entryStream);
                    ReplaceText(doc, replacements);
                    if (!hasImage)
                    {
                        RemovePictureByRelId(doc, imageRelId);
                    }
                    if (!hasMapImage)
                    {
                        RemovePictureByRelId(doc, secondaryImageRelId);
                    }
                    RemoveShapeByName(doc, secondaryImageMarkerName);
                    if (hasImage)
                    {
                        RemovePictureCropByRelId(doc, imageRelId);
                    }
                    if (hasMapImage)
                    {
                        RemovePictureCropByRelId(doc, secondaryImageRelId);
                    }
                    doc.Save(newEntryStream, System.Xml.Linq.SaveOptions.DisableFormatting);
                }
                else if (string.Equals(entry.FullName, "ppt/slides/_rels/slide1.xml.rels", StringComparison.OrdinalIgnoreCase))
                {
                    var relDoc = XDocument.Load(entryStream);
                    UpdateSlideRelationship(relDoc, imageRelId, hasImage ? targetRelPath : null);
                    UpdateSlideRelationship(relDoc, secondaryImageRelId, hasMapImage ? "../media/image2.png" : null);
                    relDoc.Save(newEntryStream, System.Xml.Linq.SaveOptions.DisableFormatting);
                }
                else
                {
                    entryStream.CopyTo(newEntryStream);
                }
            }
        }

        return outputStream.ToArray();
    }

    private static byte[] BuildCardsPptx(string templatePath, IReadOnlyList<BuildingCardPayload> payloads)
    {
        const string imageRelId = "rId3";
        const string templateImagePrefix = "ppt/media/image3.";
        const string secondaryImageRelId = "rId2";
        const string secondaryImagePath = "ppt/media/image2.png";
        const string secondaryImageMarkerName = "אליפסה 31";
        const string slideRelType = "http://schemas.openxmlformats.org/officeDocument/2006/relationships/slide";

        using var templateStream = System.IO.File.OpenRead(templatePath);
        using var templateZip = new ZipArchive(templateStream, ZipArchiveMode.Read);

        var templateImageEntry = templateZip.Entries.FirstOrDefault(entry =>
            entry.FullName.StartsWith(templateImagePrefix, StringComparison.OrdinalIgnoreCase));
        var templateImagePath = templateImageEntry?.FullName;
        var templateImageBytes = templateImageEntry is null ? null : ReadAllBytes(templateImageEntry.Open());
        var templateMapImageEntry = templateZip.GetEntry(secondaryImagePath);
        var templateMapImageBytes = templateMapImageEntry is null ? null : ReadAllBytes(templateMapImageEntry.Open());

        using var slideEntryStream = templateZip.GetEntry("ppt/slides/slide1.xml")?.Open();
        using var slideRelsEntryStream = templateZip.GetEntry("ppt/slides/_rels/slide1.xml.rels")?.Open();
        using var presentationEntryStream = templateZip.GetEntry("ppt/presentation.xml")?.Open();
        using var presentationRelsEntryStream = templateZip.GetEntry("ppt/_rels/presentation.xml.rels")?.Open();
        using var contentTypesEntryStream = templateZip.GetEntry("[Content_Types].xml")?.Open();

        if (slideEntryStream is null || slideRelsEntryStream is null ||
            presentationEntryStream is null || presentationRelsEntryStream is null ||
            contentTypesEntryStream is null)
        {
            throw new InvalidOperationException("Building card template is missing required parts.");
        }

        var slideTemplate = XDocument.Load(slideEntryStream);
        var slideRelsTemplate = XDocument.Load(slideRelsEntryStream);
        var presentationDoc = XDocument.Load(presentationEntryStream);
        var presentationRelsDoc = XDocument.Load(presentationRelsEntryStream);
        var contentTypesDoc = XDocument.Load(contentTypesEntryStream);

        var slideContentType = contentTypesDoc
            .Root?
            .Elements("{http://schemas.openxmlformats.org/package/2006/content-types}Override")
            .FirstOrDefault(element => string.Equals(
                (string?)element.Attribute("PartName"),
                "/ppt/slides/slide1.xml",
                StringComparison.OrdinalIgnoreCase))
            ?.Attribute("ContentType")
            ?.Value;

        if (string.IsNullOrWhiteSpace(slideContentType))
        {
            slideContentType = "application/vnd.openxmlformats-officedocument.presentationml.slide+xml";
        }

        using var outputStream = new MemoryStream();
        using (var outputZip = new ZipArchive(outputStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entry in templateZip.Entries)
            {
                if (string.Equals(entry.FullName, "ppt/slides/slide1.xml", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.FullName, "ppt/slides/_rels/slide1.xml.rels", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.FullName, "ppt/presentation.xml", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.FullName, "ppt/_rels/presentation.xml.rels", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.FullName, "[Content_Types].xml", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(entry.FullName, secondaryImagePath, StringComparison.OrdinalIgnoreCase) ||
                    (!string.IsNullOrWhiteSpace(templateImagePath) &&
                        string.Equals(entry.FullName, templateImagePath, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                if (string.IsNullOrEmpty(entry.Name))
                {
                    outputZip.CreateEntry(entry.FullName);
                    continue;
                }

                var newEntry = outputZip.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var newEntryStream = newEntry.Open();
                entryStream.CopyTo(newEntryStream);
            }

            RebuildPresentationSlides(presentationDoc, presentationRelsDoc, contentTypesDoc, payloads.Count, slideContentType, slideRelType);

            if (payloads.Count > 0)
            {
                for (var i = 0; i < payloads.Count; i++)
                {
                    var slideIndex = i + 1;
                    var payload = payloads[i];
                    var hasImage = payload.ImageBytes is { Length: > 0 } && !string.IsNullOrWhiteSpace(payload.ImageExtension);
                    var hasMapImage = payload.MapImageBytes is { Length: > 0 };

                    var slideDoc = new XDocument(slideTemplate);
                    ReplaceText(slideDoc, payload.Replacements);
                    if (!hasImage)
                    {
                        RemovePictureByRelId(slideDoc, imageRelId);
                    }
                    if (!hasMapImage)
                    {
                        RemovePictureByRelId(slideDoc, secondaryImageRelId);
                    }
                    RemoveShapeByName(slideDoc, secondaryImageMarkerName);
                    if (hasImage)
                    {
                        RemovePictureCropByRelId(slideDoc, imageRelId);
                    }
                    if (hasMapImage)
                    {
                        RemovePictureCropByRelId(slideDoc, secondaryImageRelId);
                    }

                    var slideEntry = outputZip.CreateEntry($"ppt/slides/slide{slideIndex}.xml", CompressionLevel.Optimal);
                    using (var slideStream = slideEntry.Open())
                    {
                        slideDoc.Save(slideStream, System.Xml.Linq.SaveOptions.DisableFormatting);
                    }

                    var slideRelsDoc = new XDocument(slideRelsTemplate);
                    string? imageTarget = null;
                    string? imagePath = null;
                    string? mapImageTarget = null;
                    string? mapImagePath = null;

                    if (hasImage)
                    {
                        var extension = payload.ImageExtension!;
                        imagePath = $"ppt/media/image3_{slideIndex}.{extension}";
                        imageTarget = $"../media/image3_{slideIndex}.{extension}";
                    }

                    if (hasMapImage)
                    {
                        mapImagePath = $"ppt/media/image2_{slideIndex}.png";
                        mapImageTarget = $"../media/image2_{slideIndex}.png";
                    }

                    UpdateSlideRelationship(slideRelsDoc, imageRelId, imageTarget);
                    UpdateSlideRelationship(slideRelsDoc, secondaryImageRelId, mapImageTarget);

                    var slideRelEntry = outputZip.CreateEntry($"ppt/slides/_rels/slide{slideIndex}.xml.rels", CompressionLevel.Optimal);
                    using (var relStream = slideRelEntry.Open())
                    {
                        slideRelsDoc.Save(relStream, System.Xml.Linq.SaveOptions.DisableFormatting);
                    }

                    if (hasImage && payload.ImageBytes is not null)
                    {
                        var outputImageBytes = payload.ImageBytes;
                        if (templateImageBytes is not null)
                        {
                            outputImageBytes = BuildLetterboxedImage(
                                payload.ImageBytes,
                                templateImageBytes,
                                payload.ImageExtension!);
                        }

                        var imageEntry = outputZip.CreateEntry(imagePath!, CompressionLevel.Optimal);
                        using var imageStream = imageEntry.Open();
                        imageStream.Write(outputImageBytes, 0, outputImageBytes.Length);
                    }

                    if (hasMapImage && payload.MapImageBytes is not null)
                    {
                        var outputMapBytes = payload.MapImageBytes;
                        if (templateMapImageBytes is not null)
                        {
                            outputMapBytes = BuildLetterboxedImage(payload.MapImageBytes, templateMapImageBytes, "png");
                        }

                        var mapImageEntry = outputZip.CreateEntry(mapImagePath!, CompressionLevel.Optimal);
                        using var mapImageStream = mapImageEntry.Open();
                        mapImageStream.Write(outputMapBytes, 0, outputMapBytes.Length);
                    }
                }
            }

            var presentationEntry = outputZip.CreateEntry("ppt/presentation.xml", CompressionLevel.Optimal);
            using (var presentationStream = presentationEntry.Open())
            {
                presentationDoc.Save(presentationStream, System.Xml.Linq.SaveOptions.DisableFormatting);
            }

            var presentationRelsEntry = outputZip.CreateEntry("ppt/_rels/presentation.xml.rels", CompressionLevel.Optimal);
            using (var presentationRelsStream = presentationRelsEntry.Open())
            {
                presentationRelsDoc.Save(presentationRelsStream, System.Xml.Linq.SaveOptions.DisableFormatting);
            }

            var contentTypesEntry = outputZip.CreateEntry("[Content_Types].xml", CompressionLevel.Optimal);
            using (var contentTypesStream = contentTypesEntry.Open())
            {
                contentTypesDoc.Save(contentTypesStream, System.Xml.Linq.SaveOptions.DisableFormatting);
            }
        }

        return outputStream.ToArray();
    }

    private static void RebuildPresentationSlides(
        XDocument presentationDoc,
        XDocument presentationRelsDoc,
        XDocument contentTypesDoc,
        int slideCount,
        string slideContentType,
        string slideRelType)
    {
        XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";
        XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";
        XNamespace ct = "http://schemas.openxmlformats.org/package/2006/content-types";

        var sldIdList = presentationDoc.Root?.Element(p + "sldIdLst");
        if (sldIdList is null && presentationDoc.Root is not null)
        {
            sldIdList = new XElement(p + "sldIdLst");
            presentationDoc.Root.Add(sldIdList);
        }

        var existingSlideIds = sldIdList?
            .Elements(p + "sldId")
            .Select(element => (int?)element.Attribute("id"))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .DefaultIfEmpty(256)
            .Max() ?? 256;

        sldIdList?.RemoveNodes();

        var relRoot = presentationRelsDoc.Root;
        if (relRoot is not null)
        {
            relRoot.Elements(rel + "Relationship")
                .Where(element => string.Equals((string?)element.Attribute("Type"), slideRelType, StringComparison.Ordinal))
                .ToList()
                .ForEach(element => element.Remove());
        }

        var maxRelId = relRoot?
            .Elements(rel + "Relationship")
            .Select(element => (string?)element.Attribute("Id"))
            .Select(ParseRelationshipId)
            .DefaultIfEmpty(0)
            .Max() ?? 0;

        var nextRelId = maxRelId + 1;
        var nextSlideId = existingSlideIds + 1;

        if (slideCount > 0 && sldIdList is not null && relRoot is not null)
        {
            for (var i = 1; i <= slideCount; i++)
            {
                var relId = $"rId{nextRelId++}";
                relRoot.Add(new XElement(rel + "Relationship",
                    new XAttribute("Id", relId),
                    new XAttribute("Type", slideRelType),
                    new XAttribute("Target", $"slides/slide{i}.xml")));

                sldIdList.Add(new XElement(p + "sldId",
                    new XAttribute("id", nextSlideId++),
                    new XAttribute(r + "id", relId)));
            }
        }

        contentTypesDoc.Root?
            .Elements(ct + "Override")
            .Where(element => ((string?)element.Attribute("PartName"))?.StartsWith("/ppt/slides/slide", StringComparison.OrdinalIgnoreCase) == true)
            .ToList()
            .ForEach(element => element.Remove());

        if (slideCount > 0 && contentTypesDoc.Root is not null)
        {
            for (var i = 1; i <= slideCount; i++)
            {
                contentTypesDoc.Root.Add(new XElement(ct + "Override",
                    new XAttribute("PartName", $"/ppt/slides/slide{i}.xml"),
                    new XAttribute("ContentType", slideContentType)));
            }
        }
    }

    private static int ParseRelationshipId(string? relId)
    {
        if (string.IsNullOrWhiteSpace(relId))
        {
            return 0;
        }

        if (!relId.StartsWith("rId", StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        return int.TryParse(relId[3..], out var value) ? value : 0;
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

    private static void RemovePictureByRelId(XDocument doc, string relId)
    {
        XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var pictures = doc
            .Descendants(p + "pic")
            .Where(pic => pic.Descendants(a + "blip")
                .Any(blip => string.Equals((string?)blip.Attribute(r + "embed"), relId, StringComparison.Ordinal)))
            .ToList();

        foreach (var picture in pictures)
        {
            picture.Remove();
        }
    }

    private static void UpdateSlideRelationship(XDocument doc, string relId, string? target)
    {
        XNamespace rel = "http://schemas.openxmlformats.org/package/2006/relationships";
        var relationship = doc.Root?
            .Elements(rel + "Relationship")
            .FirstOrDefault(node => string.Equals((string?)node.Attribute("Id"), relId, StringComparison.Ordinal));

        if (relationship is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(target))
        {
            relationship.Remove();
        }
        else
        {
            relationship.SetAttributeValue("Target", target);
        }
    }

    private static void RemoveShapeByName(XDocument doc, string shapeName)
    {
        XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";

        var shapes = doc
            .Descendants(p + "sp")
            .Where(shape =>
            {
                var cNvPr = shape.Descendants(p + "cNvPr").FirstOrDefault();
                var name = (string?)cNvPr?.Attribute("name");
                return string.Equals(name, shapeName, StringComparison.Ordinal);
            })
            .ToList();

        foreach (var shape in shapes)
        {
            shape.Remove();
        }
    }

    private static void RemovePictureCropByRelId(XDocument doc, string relId)
    {
        XNamespace p = "http://schemas.openxmlformats.org/presentationml/2006/main";
        XNamespace a = "http://schemas.openxmlformats.org/drawingml/2006/main";
        XNamespace r = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        var pictures = doc
            .Descendants(p + "pic")
            .Where(pic => pic.Descendants(a + "blip")
                .Any(blip => string.Equals((string?)blip.Attribute(r + "embed"), relId, StringComparison.Ordinal)))
            .ToList();

        foreach (var picture in pictures)
        {
            var srcRect = picture.Descendants(a + "srcRect").FirstOrDefault();
            srcRect?.Remove();
        }
    }

    private static byte[] BuildLetterboxedImage(
        byte[] sourceBytes,
        byte[] templateBytes,
        string outputExtension)
    {
        using var source = Image.Load<Rgba32>(sourceBytes);
        source.Mutate(context => context.AutoOrient());
        using var template = Image.Load<Rgba32>(templateBytes);

        var targetWidth = template.Width;
        var targetHeight = template.Height;

        var scale = Math.Min(targetWidth / (double)source.Width, targetHeight / (double)source.Height);
        var resizedWidth = Math.Max(1, (int)Math.Round(source.Width * scale));
        var resizedHeight = Math.Max(1, (int)Math.Round(source.Height * scale));

        using var resized = source.Clone(context => context.Resize(resizedWidth, resizedHeight));
        using var canvas = new Image<Rgba32>(targetWidth, targetHeight, Color.White);
        var offsetX = (targetWidth - resizedWidth) / 2;
        var offsetY = (targetHeight - resizedHeight) / 2;
        canvas.Mutate(context => context.DrawImage(resized, new Point(offsetX, offsetY), 1f));

        using var output = new MemoryStream();
        if (string.Equals(outputExtension, "png", StringComparison.OrdinalIgnoreCase))
        {
            canvas.Save(output, new PngEncoder());
        }
        else
        {
            canvas.Save(output, new JpegEncoder { Quality = 100 });
        }

        return output.ToArray();
    }


    private static byte[] ReadAllBytes(Stream stream)
    {
        using var buffer = new MemoryStream();
        stream.CopyTo(buffer);
        return buffer.ToArray();
    }

    private static string? NormalizeCardImageExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var normalized = extension.Trim().TrimStart('.').ToLowerInvariant();
        return normalized switch
        {
            "jpg" => "jpeg",
            "jpeg" => "jpeg",
            "png" => "png",
            _ => null
        };
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

    private static bool TryDecodeImageDataUrl(string raw, out byte[] bytes, out string extension)
    {
        bytes = Array.Empty<byte>();
        extension = string.Empty;

        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var commaIndex = trimmed.IndexOf(',');
        if (commaIndex < 0)
        {
            return false;
        }

        var header = trimmed[..commaIndex];
        var mimeType = string.Empty;
        var semicolonIndex = header.IndexOf(';');
        if (semicolonIndex > 5)
        {
            mimeType = header[5..semicolonIndex];
        }
        else if (header.Length > 5)
        {
            mimeType = header[5..];
        }

        extension = GetImageExtension(mimeType);
        if (string.IsNullOrWhiteSpace(extension))
        {
            return false;
        }

        var base64 = trimmed[(commaIndex + 1)..].Trim();
        if (base64.Length == 0)
        {
            return false;
        }

        try
        {
            bytes = Convert.FromBase64String(base64);
            return bytes.Length > 0;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static string GetImageExtension(string? mimeType)
    {
        if (string.IsNullOrWhiteSpace(mimeType))
        {
            return string.Empty;
        }

        return mimeType.Trim().ToLowerInvariant() switch
        {
            "image/png" => "png",
            "image/jpeg" => "jpg",
            "image/jpg" => "jpg",
            "image/webp" => "webp",
            "image/gif" => "gif",
            _ => string.Empty
        };
    }

    private static string GetImageMimeType(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return string.Empty;
        }

        var normalized = extension.Trim().TrimStart('.').ToLowerInvariant();
        return normalized switch
        {
            "png" => "image/png",
            "jpg" => "image/jpeg",
            "jpeg" => "image/jpeg",
            "webp" => "image/webp",
            "gif" => "image/gif",
            _ => string.Empty
        };
    }

    private static bool TryGetPhotoSizeBytes(string? raw, out long sizeBytes)
    {
        sizeBytes = 0;
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        var trimmed = raw.Trim();
        if (!trimmed.StartsWith("data:image", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var commaIndex = trimmed.IndexOf(',');
        var base64 = commaIndex >= 0 ? trimmed[(commaIndex + 1)..] : trimmed;
        base64 = base64.Trim();
        if (base64.Length == 0)
        {
            return false;
        }

        sizeBytes = base64.Length * 3L / 4L;
        return true;
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

    private static ImportHeaderMap BuildImportHeaderMap(
        IXLWorksheet worksheet,
        IReadOnlyDictionary<string, string> labelToColumn)
    {
        var normalizedLabelToColumn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (label, columnName) in labelToColumn)
        {
            var normalizedLabel = NormalizeExcelHeader(label);
            if (!string.IsNullOrWhiteSpace(normalizedLabel))
            {
                normalizedLabelToColumn[normalizedLabel] = columnName;
            }

            var normalizedColumn = NormalizeExcelHeader(columnName);
            if (!string.IsNullOrWhiteSpace(normalizedColumn))
            {
                normalizedLabelToColumn[normalizedColumn] = columnName;
            }
        }

        var bestMap = new Dictionary<int, string>();
        var bestHeaderRow = 0;
        var lastUsedRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var scanRows = Math.Min(lastUsedRow, 10);

        for (var rowIndex = 1; rowIndex <= scanRows; rowIndex++)
        {
            var headerRow = worksheet.Row(rowIndex);
            var lastHeaderCell = headerRow.LastCellUsed();
            if (lastHeaderCell == null)
            {
                continue;
            }

            var headerMap = new Dictionary<int, string>();
            var usedColumns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var lastHeaderColumn = lastHeaderCell.Address.ColumnNumber;
            for (var col = 1; col <= lastHeaderColumn; col++)
            {
                var rawHeader = headerRow.Cell(col).GetString();
                var header = NormalizeExcelHeader(rawHeader);
                if (string.IsNullOrWhiteSpace(header))
                {
                    continue;
                }

                if (normalizedLabelToColumn.TryGetValue(header, out var columnName) && usedColumns.Add(columnName))
                {
                    headerMap[col] = columnName;
                }
            }

            if (headerMap.Count > bestMap.Count)
            {
                bestMap = headerMap;
                bestHeaderRow = rowIndex;
            }
        }

        return new ImportHeaderMap(bestMap, bestHeaderRow);
    }

    private static string NormalizeExcelHeader(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var cleaned = value
            .Replace("\u200f", string.Empty, StringComparison.Ordinal)
            .Replace("\u200e", string.Empty, StringComparison.Ordinal)
            .Replace("\u202a", string.Empty, StringComparison.Ordinal)
            .Replace("\u202b", string.Empty, StringComparison.Ordinal)
            .Replace("\u202c", string.Empty, StringComparison.Ordinal)
            .Replace("\u00a0", " ", StringComparison.Ordinal)
            .Trim();

        cleaned = Regex.Replace(cleaned, "\\s+", " ");
        cleaned = cleaned.Replace("\"\"", "\"", StringComparison.Ordinal);
        return cleaned;
    }

    private static async Task<ImportPackage> ReadImportPackageAsync(IFormFile file, CancellationToken cancellationToken)
    {
        if (file == null)
        {
            throw new InvalidOperationException("Import file is required.");
        }

        if (!file.FileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase))
        {
            return new ImportPackage(file.OpenReadStream(), new Dictionary<int, string>(), new Dictionary<int, string>());
        }

        using var archive = new ZipArchive(file.OpenReadStream(), ZipArchiveMode.Read, leaveOpen: false);
        var excelEntry = archive.Entries.FirstOrDefault(entry =>
            string.Equals(entry.Name, "buildings.xlsx", StringComparison.OrdinalIgnoreCase))
            ?? archive.Entries.FirstOrDefault(entry =>
                entry.Name.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase));
        if (excelEntry == null)
        {
            throw new InvalidOperationException("Excel file not found inside the ZIP package.");
        }

        var excelStream = new MemoryStream();
        await using (var entryStream = excelEntry.Open())
        {
            await entryStream.CopyToAsync(excelStream, cancellationToken);
        }
        excelStream.Position = 0;

        var imagesById = new Dictionary<int, string>();
        var warningsById = new Dictionary<int, string>();

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var normalizedPath = entry.FullName.Replace('\\', '/');
            if (!normalizedPath.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var idRaw = Path.GetFileNameWithoutExtension(entry.Name);
            if (!int.TryParse(idRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var buildingId))
            {
                continue;
            }

            var extension = Path.GetExtension(entry.Name);
            var mimeType = GetImageMimeType(extension);
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                continue;
            }

            await using var imageStream = entry.Open();
            using var buffer = new MemoryStream();
            await imageStream.CopyToAsync(buffer, cancellationToken);
            var bytes = buffer.ToArray();
            if (bytes.Length == 0)
            {
                continue;
            }

            if (bytes.Length > MaxPhotoSizeBytes)
            {
                warningsById[buildingId] = "התמונה הושמטה (גדולה מ-5MB).";
                continue;
            }

            var dataUrl = $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
            imagesById[buildingId] = dataUrl;
        }

        return new ImportPackage(excelStream, imagesById, warningsById);
    }

    private static List<ImportRowData> ReadImportRows(
        IXLWorksheet worksheet,
        ImportHeaderMap headerMap,
        IReadOnlyDictionary<int, string>? imagesById = null,
        IReadOnlyDictionary<int, string>? imageWarningsById = null)
    {
        var rows = new List<ImportRowData>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var startRow = headerMap.HeaderRow > 0 ? headerMap.HeaderRow + 1 : 3;
        if (startRow <= 0)
        {
            startRow = 3;
        }

        for (var rowIndex = startRow; rowIndex <= lastRow; rowIndex++)
        {
            var row = worksheet.Row(rowIndex);
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            var hasValue = false;

            foreach (var (colIndex, columnName) in headerMap.Columns)
            {
                var raw = row.Cell(colIndex).GetValue<string>();
                var value = string.IsNullOrWhiteSpace(raw) ? null : raw.Trim();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    hasValue = true;
                }
                values[columnName] = value;
            }

            if (!hasValue)
            {
                continue;
            }

            if (values.TryGetValue("Id", out var idRaw) &&
                int.TryParse(idRaw, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsedId))
            {
                if (imagesById != null && imagesById.TryGetValue(parsedId, out var imageData))
                {
                    values[nameof(Building.PhotoUrls)] = imageData;
                }

                if (imageWarningsById != null &&
                    imageWarningsById.TryGetValue(parsedId, out var warningMessage))
                {
                    warnings.Add(warningMessage);
                }
            }

            if (values.TryGetValue(nameof(Building.PhotoUrls), out var photoValue) &&
                !string.IsNullOrWhiteSpace(photoValue) &&
                TryGetPhotoSizeBytes(photoValue, out var sizeBytes) &&
                sizeBytes > MaxPhotoSizeBytes)
            {
                warnings.Add("התמונה הושמטה (גדולה מ-5MB).");
                values[nameof(Building.PhotoUrls)] = null;
            }

            rows.Add(new ImportRowData(rowIndex, values, warnings));
        }

        return rows;
    }

    private static Dictionary<string, string?> NormalizeImportValues(IReadOnlyDictionary<string, string?> values)
    {
        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            normalized[key] = value?.Trim();
        }
        return normalized;
    }

    private static List<BuildingFieldDto> BuildImportFields(
        IReadOnlyList<BuildingFieldDto> definitions,
        IReadOnlyDictionary<string, string?> values)
    {
        return definitions
            .Select(definition =>
            {
                values.TryGetValue(definition.ColumnName, out var value);
                return new BuildingFieldDto(
                    definition.Category,
                    definition.FieldName,
                    definition.ColumnName,
                    definition.SelectTableName,
                    definition.IncludeInEventLog,
                    value,
                    null);
            })
            .ToList();
    }

    private static List<BuildingFieldDto> AlignFieldsToDefinitions(
        IReadOnlyList<BuildingFieldDto> definitions,
        IReadOnlyList<BuildingFieldDto> actual)
    {
        var actualByColumn = actual
            .Where(field => !string.IsNullOrWhiteSpace(field.ColumnName))
            .ToDictionary(field => field.ColumnName, StringComparer.OrdinalIgnoreCase);

        return definitions
            .Select(definition =>
            {
                if (actualByColumn.TryGetValue(definition.ColumnName, out var value))
                {
                    return value;
                }

                return new BuildingFieldDto(
                    definition.Category,
                    definition.FieldName,
                    definition.ColumnName,
                    definition.SelectTableName,
                    definition.IncludeInEventLog,
                    null,
                    null);
            })
            .ToList();
    }

    private static ImportExistingMatch BuildExistingMatch(Building building, IReadOnlyList<BuildingFieldDto> definitions)
    {
        var fields = AlignFieldsToDefinitions(definitions, BuildFieldsSnapshot(building, includePhotos: true));
        return new ImportExistingMatch(
            building.Id,
            building.StreetName,
            building.HouseNumber ?? string.Empty,
            building.BuildingName ?? string.Empty,
            fields);
    }

    private static bool IsExactMatch(
        IReadOnlyList<BuildingFieldDto> definitions,
        IReadOnlyDictionary<string, string?> values,
        ImportExistingMatch match)
    {
        var existingByColumn = match.Fields
            .Where(field => !string.IsNullOrWhiteSpace(field.ColumnName))
            .ToDictionary(field => field.ColumnName, StringComparer.OrdinalIgnoreCase);

        foreach (var definition in definitions)
        {
            if (string.IsNullOrWhiteSpace(definition.ColumnName))
            {
                continue;
            }

            if (string.Equals(definition.ColumnName, "StreetName", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var importValue = ResolveImportComparisonValue(definition, values);
            existingByColumn.TryGetValue(definition.ColumnName, out var existingField);
            var existingValue = NormalizeComparisonValue(existingField?.Value);

            if (!string.Equals(importValue, existingValue, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static string ResolveImportComparisonValue(
        BuildingFieldDto definition,
        IReadOnlyDictionary<string, string?> values)
    {
        values.TryGetValue(definition.ColumnName, out var rawValue);
        if (string.IsNullOrWhiteSpace(rawValue))
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(definition.SelectTableName))
        {
            var resolved = TryResolveSelectValue(rawValue, definition.SelectTableName);
            if (resolved.HasValue)
            {
                var label = SelectTables
                    .GetOptions(definition.SelectTableName)
                    .FirstOrDefault(option => option.Value == resolved.Value)
                    ?.Label;
                return NormalizeComparisonValue(label);
            }
        }

        return NormalizeComparisonValue(rawValue);
    }

    private static string NormalizeComparisonValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();
    }

    private static int? TryParsePositiveInt(string? raw)
    {
        return BuildingRules.TryParsePositiveInt(raw);
    }

    private static int? TryParseStreetId(string? raw)
    {
        return BuildingRules.TryParseStreetId(raw);
    }

    private static int? ResolveRehabSivugValue()
    {
        return BuildingRules.ResolveRehabSivugValue();
    }

    private static int? TryResolveSelectValue(string? raw, string tableName)
    {
        return BuildingRules.TryResolveSelectValue(raw, tableName);
    }

    private static List<string> GetMissingRequiredColumns(
        IReadOnlyDictionary<string, string?> values,
        int? rehabSivugValue,
        List<string> warnings,
        bool requireId)
    {
        return BuildingRules.GetMissingRequiredColumns(values, rehabSivugValue, warnings, requireId);
    }

    private sealed record BuildingSnapshot(
        int Id,
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
