using System.Globalization;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;
using WebServer.Services;

namespace WebServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StreetsController : ControllerBase
{
    private readonly AppDbContext _context;

    public StreetsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<IEnumerable<StreetDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = _context.Streets
            .Where(s => s.StreetId != StreetRules.ReservedNoStreetId)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{search}%"));
        }

        var items = await query
            .OrderBy(s => s.Name)
            .ThenBy(s => s.StreetId)
            .Select(s => new StreetDto(s.StreetId, s.Name))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<StreetDto>> Get(int id, CancellationToken cancellationToken)
    {
        if (id == StreetRules.ReservedNoStreetId)
        {
            return NotFound();
        }

        var street = await _context.Streets.FirstOrDefaultAsync(s => s.StreetId == id, cancellationToken);
        if (street == null)
        {
            return NotFound();
        }

        return new StreetDto(street.StreetId, street.Name);
    }

    [HttpPost]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult<StreetDto>> Create([FromBody] StreetEditRequest request, CancellationToken cancellationToken)
    {
        var validation = StreetRules.ValidateValues(request.StreetId.ToString(CultureInfo.InvariantCulture), request.Name, true);
        if (validation.MissingRequired.Count > 0 || validation.InvalidValues.Count > 0)
        {
            return BadRequest(new
            {
                error = "שדות חובה חסרים או שגויים.",
                missingRequired = validation.MissingRequired,
                invalidValues = validation.InvalidValues
            });
        }

        if (validation.StreetId.HasValue)
        {
            var idExists = await _context.Streets.AnyAsync(s => s.StreetId == validation.StreetId.Value, cancellationToken);
            if (idExists)
            {
                return Conflict(new { error = "קיים רחוב עם מזהה זה", isIdDuplicate = true });
            }
        }

        var street = new Street
        {
            StreetId = validation.StreetId!.Value,
            Name = validation.Name ?? string.Empty
        };

        _context.Streets.Add(street);
        await _context.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(Get), new { id = street.StreetId }, new StreetDto(street.StreetId, street.Name));
    }

    [HttpPut("{id:int}")]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult> Update(int id, [FromBody] StreetEditRequest request, CancellationToken cancellationToken)
    {
        if (id != request.StreetId)
        {
            return BadRequest("StreetId mismatch.");
        }

        var validation = StreetRules.ValidateValues(request.StreetId.ToString(CultureInfo.InvariantCulture), request.Name, true);
        if (validation.MissingRequired.Count > 0 || validation.InvalidValues.Count > 0)
        {
            return BadRequest(new
            {
                error = "שדות חובה חסרים או שגויים.",
                missingRequired = validation.MissingRequired,
                invalidValues = validation.InvalidValues
            });
        }

        var street = await _context.Streets.FirstOrDefaultAsync(s => s.StreetId == id, cancellationToken);
        if (street == null)
        {
            return NotFound();
        }

        street.Name = validation.Name ?? string.Empty;
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var street = await _context.Streets.FirstOrDefaultAsync(s => s.StreetId == id, cancellationToken);
        if (street == null)
        {
            return NotFound();
        }

        _context.Streets.Remove(street);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    public sealed record StreetImportExistingMatch(int StreetId, string Name);
    public sealed record StreetImportValidationIssue(string Field, string Message);
    public sealed record StreetImportPreviewRow(
        int RowNumber,
        Dictionary<string, string?> Values,
        StreetImportExistingMatch? IdMatch,
        bool HasIdConflict,
        bool ExactMatch,
        List<string> MissingRequired,
        List<StreetImportValidationIssue> InvalidValues,
        List<string> Warnings);
    public sealed record StreetImportPreviewResponse(List<StreetImportPreviewRow> Rows);
    public sealed record StreetImportValidateRequest(Dictionary<string, string?> Values);
    public sealed record StreetImportApplyRow(int RowNumber, string Action, Dictionary<string, string?> Values);
    public sealed record StreetImportApplyRequest(List<StreetImportApplyRow> Rows);
    public sealed record StreetsExportRequest(List<int> StreetIds);
    private sealed record StreetImportRowData(int RowNumber, Dictionary<string, string?> Values, List<string> Warnings);
    private sealed record StreetHeaderMap(Dictionary<int, string> Columns, int HeaderRow);

    [HttpGet("export")]
    [Authorize(Policy = "Viewer")]
    public async Task<IActionResult> ExportStreets(CancellationToken cancellationToken)
    {
        var streets = await _context.Streets
            .Where(s => s.StreetId != StreetRules.ReservedNoStreetId)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.StreetId)
            .ToListAsync(cancellationToken);

        return BuildStreetsExport(streets);
    }

    [HttpPost("export")]
    [Authorize(Policy = "Viewer")]
    public async Task<IActionResult> ExportSelectedStreets(
        [FromBody] StreetsExportRequest? request,
        CancellationToken cancellationToken)
    {
        if (request?.StreetIds is null || request.StreetIds.Count == 0)
        {
            return BuildStreetsExport(Array.Empty<Street>());
        }

        var ids = request.StreetIds.Distinct().ToList();
        var streets = await _context.Streets
            .Where(s => ids.Contains(s.StreetId))
            .Where(s => s.StreetId != StreetRules.ReservedNoStreetId)
            .OrderBy(s => s.Name)
            .ThenBy(s => s.StreetId)
            .ToListAsync(cancellationToken);

        return BuildStreetsExport(streets);
    }

    [HttpPost("import/preview")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<StreetImportPreviewResponse>> PreviewImport(
        [FromForm] IFormFile? file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Import file is required.");
        }

        using var stream = file.OpenReadStream();
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.FirstOrDefault();
        if (worksheet == null)
        {
            return BadRequest("Excel worksheet not found.");
        }

        var headerMap = BuildStreetHeaderMap(worksheet);
        if (headerMap.Columns.Count == 0)
        {
            return BadRequest("Excel headers do not match the export format.");
        }

        var importRows = ReadStreetImportRows(worksheet, headerMap);
        if (importRows.Count == 0)
        {
            return BadRequest("No data rows found.");
        }

        var normalizedRows = importRows
            .Select(row => (row.RowNumber, Values: NormalizeStreetValues(row.Values), row.Warnings))
            .ToList();

        var idCounts = normalizedRows
            .Select(row => StreetRules.TryParseStreetId(row.Values.TryGetValue("StreetId", out var raw) ? raw : null))
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .GroupBy(id => id)
            .ToDictionary(group => group.Key, group => group.Count());

        var requestedIds = idCounts.Keys.ToList();
        var existingById = requestedIds.Count == 0
            ? new Dictionary<int, Street>()
            : await _context.Streets
                .Where(s => requestedIds.Contains(s.StreetId))
                .ToDictionaryAsync(s => s.StreetId, cancellationToken);

        var previewRows = new List<StreetImportPreviewRow>();

        foreach (var row in normalizedRows)
        {
            var warnings = new List<string>(row.Warnings);
            row.Values.TryGetValue("StreetId", out var idRaw);
            row.Values.TryGetValue("Name", out var nameRaw);

            var validation = StreetRules.ValidateValues(idRaw, nameRaw, true);
            var invalidValues = validation.InvalidValues
                .Select(issue => new StreetImportValidationIssue(issue.Field, issue.Message))
                .ToList();

            var streetId = validation.StreetId;
            StreetImportExistingMatch? idMatch = null;
            bool exactMatch = false;
            if (streetId.HasValue && existingById.TryGetValue(streetId.Value, out var existing))
            {
                idMatch = new StreetImportExistingMatch(existing.StreetId, existing.Name);
                exactMatch = string.Equals(existing.Name?.Trim(), validation.Name?.Trim(), StringComparison.Ordinal);
            }

            var hasIdConflict = idMatch != null && !exactMatch;
            if (streetId.HasValue && idCounts.TryGetValue(streetId.Value, out var count) && count > 1)
            {
                hasIdConflict = true;
                warnings.Add("מזהה רחוב מופיע יותר מפעם אחת בקובץ הייבוא.");
            }

            previewRows.Add(new StreetImportPreviewRow(
                row.RowNumber,
                row.Values,
                idMatch,
                hasIdConflict,
                exactMatch,
                validation.MissingRequired,
                invalidValues,
                warnings));
        }

        return Ok(new StreetImportPreviewResponse(previewRows));
    }

    [HttpPost("import/validate")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult<StreetImportPreviewRow>> ValidateImportRow(
        [FromBody] StreetImportValidateRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Values is null || request.Values.Count == 0)
        {
            return BadRequest("No values supplied.");
        }

        var values = NormalizeStreetValues(request.Values);
        values.TryGetValue("StreetId", out var idRaw);
        values.TryGetValue("Name", out var nameRaw);

        var validation = StreetRules.ValidateValues(idRaw, nameRaw, true);
        var invalidValues = validation.InvalidValues
            .Select(issue => new StreetImportValidationIssue(issue.Field, issue.Message))
            .ToList();

        StreetImportExistingMatch? idMatch = null;
        bool exactMatch = false;
        var warnings = new List<string>();
        if (validation.StreetId.HasValue)
        {
            var existing = await _context.Streets.FirstOrDefaultAsync(
                s => s.StreetId == validation.StreetId.Value,
                cancellationToken);
            if (existing != null)
            {
                idMatch = new StreetImportExistingMatch(existing.StreetId, existing.Name);
                exactMatch = string.Equals(existing.Name?.Trim(), validation.Name?.Trim(), StringComparison.Ordinal);
            }
        }

        var hasIdConflict = idMatch != null && !exactMatch;

        return Ok(new StreetImportPreviewRow(
            0,
            values,
            idMatch,
            hasIdConflict,
            exactMatch,
            validation.MissingRequired,
            invalidValues,
            warnings));
    }

    [HttpPost("import/apply")]
    [Authorize(Policy = "Admin")]
    public async Task<IActionResult> ApplyImport(
        [FromBody] StreetImportApplyRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Rows is null || request.Rows.Count == 0)
        {
            return BadRequest("No import rows supplied.");
        }

        var duplicateIds = request.Rows
            .Where(row => !string.Equals(row.Action?.Trim(), "skip", StringComparison.OrdinalIgnoreCase))
            .Select(row =>
            {
                row.Values.TryGetValue("StreetId", out var raw);
                return new { row.RowNumber, Id = StreetRules.TryParseStreetId(raw) };
            })
            .Where(entry => entry.Id.HasValue)
            .GroupBy(entry => entry.Id!.Value)
            .Where(group => group.Count() > 1)
            .ToList();

        if (duplicateIds.Count > 0)
        {
            return Conflict(new
            {
                error = "מזהה רחוב מופיע יותר מפעם אחת בקובץ הייבוא.",
                isIdDuplicate = true,
                duplicates = duplicateIds.Select(group => new
                {
                    Id = group.Key,
                    Rows = group.Select(entry => entry.RowNumber).ToList()
                })
            });
        }

        var created = 0;
        var replaced = 0;
        var skipped = 0;

        foreach (var row in request.Rows)
        {
            var action = row.Action?.Trim().ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(action))
            {
                return BadRequest($"Row {row.RowNumber}: Action is required.");
            }

            if (action == "skip")
            {
                skipped++;
                continue;
            }

            if (action != "create" && action != "replace")
            {
                return BadRequest($"Row {row.RowNumber}: Unsupported action '{row.Action}'.");
            }

            var values = NormalizeStreetValues(row.Values ?? new Dictionary<string, string?>());
            values.TryGetValue("StreetId", out var idRaw);
            values.TryGetValue("Name", out var nameRaw);

            var validation = StreetRules.ValidateValues(idRaw, nameRaw, true);
            if (validation.MissingRequired.Count > 0 || validation.InvalidValues.Count > 0)
            {
                return BadRequest(new
                {
                    error = "שדות חובה חסרים או שגויים.",
                    row.RowNumber,
                    missingRequired = validation.MissingRequired,
                    invalidValues = validation.InvalidValues.Select(issue => new StreetImportValidationIssue(issue.Field, issue.Message)).ToList()
                });
            }

            var streetId = validation.StreetId!.Value;
            var name = validation.Name ?? string.Empty;

            if (action == "replace")
            {
                var existing = await _context.Streets.FirstOrDefaultAsync(
                    s => s.StreetId == streetId,
                    cancellationToken);
                if (existing == null)
                {
                    return BadRequest($"Row {row.RowNumber}: StreetId not found for replace.");
                }

                _context.Streets.Remove(existing);
                _context.Streets.Add(new Street { StreetId = streetId, Name = name });
                replaced++;
                continue;
            }

            var idExists = await _context.Streets.AnyAsync(s => s.StreetId == streetId, cancellationToken);
            if (idExists)
            {
                return Conflict(new { error = "קיים רחוב עם מזהה זה", row.RowNumber, isIdDuplicate = true });
            }

            _context.Streets.Add(new Street
            {
                StreetId = streetId,
                Name = name
            });
            created++;
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Ok(new { created, replaced, skipped });
    }

    [HttpPost("convert-template")]
    [Authorize(Policy = "Admin")]
    public IActionResult ConvertStreetsTemplate([FromForm] IFormFile? file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("Import file is required.");
        }

        using var stream = file.OpenReadStream();
        var streets = StreetsExcelImporter.ReadStreetsFromStream(stream, out var error);
        if (!string.IsNullOrWhiteSpace(error))
        {
            return BadRequest(error);
        }

        return BuildStreetsExport(streets);
    }

    private static IActionResult BuildStreetsExport(IReadOnlyList<Street> streets)
    {
        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Streets");

        worksheet.Cell(1, 1).Value = "רחובות";
        worksheet.Range(1, 1, 1, 2).Merge();
        worksheet.Row(1).Style.Font.Bold = true;

        worksheet.Cell(2, 1).Value = "מזהה רחוב *";
        worksheet.Cell(2, 2).Value = "שם רחוב *";
        worksheet.Row(2).Style.Font.Bold = true;

        for (var i = 0; i < streets.Count; i++)
        {
            var row = i + 3;
            worksheet.Cell(row, 1).Value = streets[i].StreetId;
            worksheet.Cell(row, 2).Value = streets[i].Name ?? string.Empty;
        }

        worksheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        var fileName = $"streets-{DateTimeOffset.UtcNow:yyyy-MM-dd}.xlsx";
        return new FileContentResult(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
        {
            FileDownloadName = fileName
        };
    }

    private static StreetHeaderMap BuildStreetHeaderMap(IXLWorksheet worksheet)
    {
        var labelToColumn = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [NormalizeExcelHeader("מזהה רחוב")] = "StreetId",
            [NormalizeExcelHeader("שם רחוב")] = "Name",
            [NormalizeExcelHeader("מזהה רחוב *")] = "StreetId",
            [NormalizeExcelHeader("שם רחוב *")] = "Name"
        };

        var lastRow = Math.Min(worksheet.LastRowUsed()?.RowNumber() ?? 0, 10);
        for (var rowIndex = 1; rowIndex <= lastRow; rowIndex++)
        {
            var row = worksheet.Row(rowIndex);
            var columns = new Dictionary<int, string>();

            foreach (var cell in row.CellsUsed())
            {
                var raw = cell.GetString();
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                var normalized = NormalizeExcelHeader(raw);
                if (labelToColumn.TryGetValue(normalized, out var columnName))
                {
                    columns[cell.Address.ColumnNumber] = columnName;
                }
            }

            if (columns.Values.Distinct(StringComparer.OrdinalIgnoreCase).Count() >= 2)
            {
                return new StreetHeaderMap(columns, rowIndex);
            }
        }

        return new StreetHeaderMap(new Dictionary<int, string>(), 0);
    }

    private static List<StreetImportRowData> ReadStreetImportRows(IXLWorksheet worksheet, StreetHeaderMap headerMap)
    {
        var rows = new List<StreetImportRowData>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 0;
        var startRow = headerMap.HeaderRow > 0 ? headerMap.HeaderRow + 1 : 2;

        for (var rowIndex = startRow; rowIndex <= lastRow; rowIndex++)
        {
            var row = worksheet.Row(rowIndex);
            var values = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            var warnings = new List<string>();
            var hasValue = false;

            foreach (var (colIndex, columnName) in headerMap.Columns)
            {
                var raw = row.Cell(colIndex).GetString();
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

            rows.Add(new StreetImportRowData(rowIndex, values, warnings));
        }

        return rows;
    }

    private static Dictionary<string, string?> NormalizeStreetValues(IReadOnlyDictionary<string, string?> values)
    {
        var normalized = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in values)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            normalized[key] = string.IsNullOrWhiteSpace(value) ? null : value?.Trim();
        }
        return normalized;
    }

    private static string NormalizeExcelHeader(string header)
    {
        if (string.IsNullOrWhiteSpace(header))
        {
            return string.Empty;
        }

        var sanitized = header.Trim();
        sanitized = sanitized.Replace("*", string.Empty, StringComparison.Ordinal);
        var openParen = sanitized.IndexOf('(');
        if (openParen >= 0)
        {
            sanitized = sanitized[..openParen];
        }

        return sanitized.Trim();
    }
}
