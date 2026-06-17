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
        var query = from log in _context.BuildingLogs.Include(l => l.CreatedByUser)
                    join building in _context.Buildings on log.BuildingId equals building.Id into buildingGroup
                    from building in buildingGroup.DefaultIfEmpty()
                    select new { Log = log, Building = building };

        if (filter.BuildingId.HasValue)
        {
            query = query.Where(x => x.Log.BuildingId == filter.BuildingId.Value);
        }

        if (filter.UserId.HasValue)
        {
            query = query.Where(x => x.Log.CreatedByUserId == filter.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.User))
        {
            query = query.Where(x =>
                x.Log.CreatedByUser != null &&
                EF.Functions.ILike(x.Log.CreatedByUser.Username, $"%{filter.User}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.LogType))
        {
            var logType = filter.LogType.Trim().ToLowerInvariant();
            if (logType == "deleted")
            {
                query = query.Where(x =>
                    x.Log.Category == "מחיקה" ||
                    x.Log.Title == "מחיקת מבנה");
            }
            else if (logType == "created")
            {
                query = query.Where(x =>
                    x.Log.Category == "Create" ||
                    x.Log.Category == "יצירה" ||
                    x.Log.Title == "יצירת מבנה" ||
                    x.Log.Title == "שחזור מבנה");
            }
        }

        if (filter.From.HasValue)
        {
            query = query.Where(x => x.Log.CreatedAt >= filter.From.Value);
        }

        if (filter.To.HasValue)
        {
            query = query.Where(x => x.Log.CreatedAt <= filter.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Street))
        {
            query = query.Where(x =>
                x.Building != null &&
                EF.Functions.ILike(x.Building.StreetName, $"%{filter.Street}%"));
        }

        if (filter.StreetId.HasValue)
        {
            query = query.Where(x =>
                x.Building != null &&
                x.Building.StreetCode == filter.StreetId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.HouseNumber))
        {
            query = query.Where(x =>
                x.Building != null &&
                x.Building.HouseNumber == filter.HouseNumber);
        }

        if (!string.IsNullOrWhiteSpace(filter.Name))
        {
            query = query.Where(x =>
                x.Building != null &&
                EF.Functions.ILike(x.Building.BuildingName, $"%{filter.Name}%"));
        }

        if (filter.Status.HasValue)
        {
            query = query.Where(x =>
                x.Building != null &&
                x.Building.ShikumStatus == filter.Status.Value);
        }

        if (filter.BldSivug.HasValue)
        {
            query = query.Where(x =>
                x.Building != null &&
                x.Building.BldSivug == filter.BldSivug.Value);
        }

        if (filter.SugBaalut.HasValue)
        {
            query = query.Where(x =>
                x.Building != null &&
                x.Building.SugBaalut == filter.SugBaalut.Value);
        }

        if (!string.IsNullOrWhiteSpace(filter.Quarter))
        {
            query = query.Where(x =>
                x.Building != null &&
                EF.Functions.ILike(x.Building.Quarter ?? string.Empty, $"%{filter.Quarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.SubQuarter))
        {
            query = query.Where(x =>
                x.Building != null &&
                EF.Functions.ILike(x.Building.SubQuarter ?? string.Empty, $"%{filter.SubQuarter}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatisticalArea))
        {
            query = query.Where(x =>
                x.Building != null &&
                EF.Functions.ILike(x.Building.StatisticalArea ?? string.Empty, $"%{filter.StatisticalArea}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
        {
            query = query.Where(x =>
                x.Building != null &&
                EF.Functions.ILike(x.Building.Neighborhood ?? string.Empty, $"%{filter.Neighborhood}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatusSummary))
        {
            query = query.Where(x =>
                x.Building != null &&
                EF.Functions.ILike(x.Building.StatusSummary ?? string.Empty, $"%{filter.StatusSummary}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.Log.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(x => new BuildingLogDto(
                x.Log.Id,
                x.Log.BuildingId,
                x.Log.Title,
                x.Log.Message,
                x.Log.Category,
                x.Log.Severity,
                IsraelTime.Convert(x.Log.CreatedAt),
                x.Log.CreatedByUser != null ? x.Log.CreatedByUser.Username : null,
                x.Building != null ? x.Building.StreetName : null,
                x.Building != null ? x.Building.HouseNumber : null,
                x.Building != null ? x.Building.BuildingName : null,
                x.Building != null ? x.Building.Neighborhood : null,
                x.Building != null ? x.Building.BldSivug : null,
                x.Building != null ? x.Building.ShikumStatus : null,
                x.Building != null ? x.Building.StatusSummary : null,
                x.Building != null ? x.Building.SugBaalut : null,
                x.Building != null ? x.Building.Quarter : null,
                x.Building != null ? x.Building.SubQuarter : null,
                x.Building != null ? x.Building.StatisticalArea : null))
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResult<BuildingLogDto>(items, total, filter.Page, filter.PageSize));
    }

    [HttpGet("building/{buildingId:int}")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<IEnumerable<BuildingLogDto>>> GetBuildingLogs(int buildingId, CancellationToken cancellationToken)
    {
        var logs = await (
            from log in _context.BuildingLogs.Where(l => l.BuildingId == buildingId).Include(l => l.CreatedByUser)
            join building in _context.Buildings on log.BuildingId equals building.Id into buildingGroup
            from building in buildingGroup.DefaultIfEmpty()
            orderby log.CreatedAt descending
            select new BuildingLogDto(
                log.Id,
                log.BuildingId,
                log.Title,
                log.Message,
                log.Category,
                log.Severity,
                IsraelTime.Convert(log.CreatedAt),
                log.CreatedByUser != null ? log.CreatedByUser.Username : null,
                building != null ? building.StreetName : null,
                building != null ? building.HouseNumber : null,
                building != null ? building.BuildingName : null,
                building != null ? building.Neighborhood : null,
                building != null ? building.BldSivug : null,
                building != null ? building.ShikumStatus : null,
                building != null ? building.StatusSummary : null,
                building != null ? building.SugBaalut : null,
                building != null ? building.Quarter : null,
                building != null ? building.SubQuarter : null,
                building != null ? building.StatisticalArea : null))
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
            building.BldSivug,
            building.ShikumStatus,
            building.StatusSummary,
            building.SugBaalut,
            building.Quarter,
            building.SubQuarter,
            building.StatisticalArea);
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

}
