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
        [FromQuery] LogFilterParameters filter,
        CancellationToken cancellationToken = default)
    {
        var query = _context.BuildingLogs
            .Include(l => l.CreatedByUser)
            .Include(l => l.Building)
            .AsQueryable();

        if (filter.BuildingId.HasValue)
        {
            query = query.Where(l => l.BuildingId == filter.BuildingId.Value);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(l => l.CreatedByUserId == filter.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.User))
        {
            query = query.Where(l => l.CreatedByUser != null && EF.Functions.ILike(l.CreatedByUser.Username, $"%{filter.User}%"));
        }

        if (filter.From.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(l => l.CreatedAt <= filter.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Street))
        {
            query = query.Where(l => EF.Functions.ILike(l.Building.StreetName, $"%{filter.Street}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.HouseNumber))
        {
            query = query.Where(l => l.Building.HouseNumber == filter.HouseNumber);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(l => EF.Functions.ILike(l.Building.BuildingName, $"%{filter.Name}%"));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(l => l.Building.ShikumStatus == filter.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
        {
            query = query.Where(l => EF.Functions.ILike(l.Building.Neighborhood, $"%{filter.Neighborhood}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatusSummary))
        {
            query = query.Where(l => EF.Functions.ILike(l.Building.StatusSummary, $"%{filter.StatusSummary}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
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
                l.Building.ShikumStatus.ToString(),
                l.Building.StatusSummary))
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResult<BuildingLogDto>(items, total, filter.Page, filter.PageSize));
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
                l.Building.ShikumStatus.ToString(),
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

        Guid? actorId = await ResolveActorIdAsync(cancellationToken);

        var log = new BuildingLog
        {
            BuildingId = buildingId,
            Title = request.Title,
            Message = request.Message,
            Category = request.Category,
            Severity = request.Severity,
            CreatedByUserId = actorId,
            CreatedAt = IsraelTime.NowUtc
        };

        _context.BuildingLogs.Add(log);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(BuildingLog), log.Id.ToString(), "Create", request, cancellationToken);

        string? createdBy = null;
        if (actorId.HasValue)
        {
            createdBy = await _context.Users
                .Where(u => u.Id == actorId.Value)
                .Select(u => u.Username)
                .FirstOrDefaultAsync(cancellationToken);
        }

        var dto = new BuildingLogDto(
            log.Id,
            log.BuildingId,
            log.Title,
            log.Message,
            log.Category,
            log.Severity,
            IsraelTime.Convert(log.CreatedAt),
            createdBy,
            building.StreetName,
            building.HouseNumber,
            building.BuildingName,
            building.Neighborhood,
            building.ShikumStatus.ToString(),
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
