using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;
using WebServer.Services;
using WebServer.Utilities;

namespace WebServer.Controllers;

[Route("api/[controller]")]
public class LogsController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly IAuditService _auditService;

    public LogsController(AppDbContext context, IAuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    [HttpGet]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<PaginatedResult<BuildingLogDto>>> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? buildingId = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? user = null,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] string? street = null,
        [FromQuery] string? houseNumber = null,
        [FromQuery] string? nickname = null,
        [FromQuery] string? status = null,
        [FromQuery] string? neighborhood = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BuildingLogs
            .Include(l => l.CreatedByUser)
            .Include(l => l.Building)
            .AsQueryable();

        if (buildingId.HasValue)
        {
            query = query.Where(l => l.BuildingId == buildingId.Value);
        }

        if (userId.HasValue)
        {
            query = query.Where(l => l.CreatedByUserId == userId.Value);
        }

        if (!string.IsNullOrWhiteSpace(user))
        {
            query = query.Where(l => l.CreatedByUser != null && EF.Functions.ILike(l.CreatedByUser.Username, $"%{user}%"));
        }

        if (from.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= to.Value);
        }

        if (!string.IsNullOrWhiteSpace(street))
        {
            query = query.Where(l => EF.Functions.ILike(l.Building.StreetName, $"%{street}%"));
        }

        if (!string.IsNullOrWhiteSpace(houseNumber))
        {
            query = query.Where(l => l.Building.HouseNumber == houseNumber);
        }

        if (!string.IsNullOrWhiteSpace(nickname))
        {
            query = query.Where(l => EF.Functions.ILike(l.Building.BuildingName, $"%{nickname}%"));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(l => EF.Functions.ILike(l.Building.ShikumStatus, $"%{status}%"));
        }

        if (!string.IsNullOrWhiteSpace(neighborhood))
        {
            query = query.Where(l => EF.Functions.ILike(l.Building.Neighborhood, $"%{neighborhood}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new BuildingLogDto(
                l.Id,
                l.BuildingId,
                l.Title,
                l.Message,
                l.Category,
                l.Severity,
                IsraelTime.Convert(l.CreatedAt),
                l.CreatedByUser != null ? l.CreatedByUser.Username : null,
                l.Building.StreetName,
                l.Building.HouseNumber,
                l.Building.BuildingName,
                l.Building.Neighborhood,
                l.Building.ShikumStatus,
                l.Building.StatusSummary))
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResult<BuildingLogDto>(items, total, page, pageSize));
    }

    [HttpGet("building/{buildingId:int}")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<IEnumerable<BuildingLogDto>>> GetBuildingLogs(int buildingId, CancellationToken cancellationToken)
    {
        var logs = await _context.BuildingLogs
            .Where(l => l.BuildingId == buildingId)
            .Include(l => l.CreatedByUser)
            .Include(l => l.Building)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new BuildingLogDto(
                l.Id,
                l.BuildingId,
                l.Title,
                l.Message,
                l.Category,
                l.Severity,
                IsraelTime.Convert(l.CreatedAt),
                l.CreatedByUser != null ? l.CreatedByUser.Username : null,
                l.Building.StreetName,
                l.Building.HouseNumber,
                l.Building.BuildingName,
                l.Building.Neighborhood,
                l.Building.ShikumStatus,
                l.Building.StatusSummary))
            .ToListAsync(cancellationToken);

        return Ok(logs);
    }

    [HttpPost("building/{buildingId:int}")]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult<BuildingLogDto>> CreateLog(int buildingId, [FromBody] BuildingLogRequest request, CancellationToken cancellationToken)
    {
        var building = await _context.Buildings.FindAsync(new object[] { buildingId }, cancellationToken);
        if (building is null)
        {
            return NotFound();
        }

        var log = new BuildingLog
        {
            BuildingId = buildingId,
            Title = request.Title,
            Message = request.Message,
            Category = request.Category,
            Severity = request.Severity,
            CreatedByUserId = CurrentUserId,
            CreatedAt = IsraelTime.NowUtc
        };

        _context.BuildingLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(BuildingLog), log.Id.ToString(), "Create", request, cancellationToken);

        var dto = new BuildingLogDto(
            log.Id,
            log.BuildingId,
            log.Title,
            log.Message,
            log.Category,
            log.Severity,
            IsraelTime.Convert(log.CreatedAt),
            null,
            building.StreetName,
            building.HouseNumber,
            building.BuildingName,
            building.Neighborhood,
            building.ShikumStatus,
            building.StatusSummary);
        return CreatedAtAction(nameof(GetBuildingLogs), new { buildingId }, dto);
    }

    [HttpPut("{logId:int}")]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult> UpdateLog(int logId, [FromBody] BuildingLogRequest request, CancellationToken cancellationToken)
    {
        var log = await _context.BuildingLogs.FindAsync(new object[] { logId }, cancellationToken);
        if (log is null)
        {
            return NotFound();
        }

        log.Title = request.Title;
        log.Message = request.Message;
        log.Category = request.Category;
        log.Severity = request.Severity;
        log.UpdatedAt = IsraelTime.NowUtc;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(BuildingLog), log.Id.ToString(), "Update", request, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{logId:int}")]
    [Authorize(Policy = "Admin")]
    public async Task<ActionResult> DeleteLog(int logId, CancellationToken cancellationToken)
    {
        var log = await _context.BuildingLogs.FindAsync(new object[] { logId }, cancellationToken);
        if (log is null)
        {
            return NotFound();
        }

        log.IsDeleted = true;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(BuildingLog), logId.ToString(), "Delete", null, cancellationToken);
        return NoContent();
    }
}
