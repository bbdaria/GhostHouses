using System.Text.Json;
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
public class BuildingsController : ApiControllerBase
{
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

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
        {
            query = query.Where(b => EF.Functions.ILike(b.Neighborhood, $"%{filter.Neighborhood}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.StatusSummary))
        {
            query = query.Where(b => EF.Functions.ILike(b.StatusSummary, $"%{filter.StatusSummary}%"));
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
                b.BuildingName,
                b.StreetName,
                b.HouseNumber,
                b.Neighborhood,
                b.ShikumStatus,
                b.BldSivug,
                b.StatusSummary))
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResult<BuildingSummaryDto>(items, total, filter.Page, filter.PageSize));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<BuildingDetailDto>> GetBuilding(int id, CancellationToken cancellationToken)
    {
        var building = await _context.Buildings
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
                building.ShikumStatus.ToString(),
                building.StatusSummary))
            .ToList();

        var detail = new BuildingDetailDto(
            new BuildingSummaryDto(building.Id, building.FldId, building.BuildingName, building.StreetName, building.HouseNumber, building.Neighborhood, building.ShikumStatus, building.BldSivug, building.StatusSummary),
            building.StatusSummary,
            IsraelTime.Convert(building.StatusSummaryUpdatedAt),
            building.Complaints,
            string.IsNullOrWhiteSpace(building.PhotoUrls) ? Array.Empty<string>() : building.PhotoUrls.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
            externalData,
            logs);

        return Ok(detail);
    }

    [HttpPost]
    [Authorize(Policy = "Editor")]
    public async Task<ActionResult<BuildingSummaryDto>> CreateBuilding([FromBody] BuildingEditRequest request, CancellationToken cancellationToken)
    {
        var building = new Building
        {
            FldId = request.FldId,
            StreetName = request.StreetName,
            HouseNumber = request.HouseNumber,
            BuildingName = request.BuildingName,
            Neighborhood = request.Neighborhood,
            BldSivug = request.BldSivug ?? "Unclassified",
            ShikumStatus = request.ShikumStatus ?? BuildingStatus.Unknown,
            StatusSummary = request.StatusSummary ?? string.Empty,
            Complaints = request.Complaints ?? string.Empty,
            PhotoUrls = request.Photos is null ? string.Empty : string.Join(',', request.Photos)
        };

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync(cancellationToken);

        Guid? actorId = await ResolveActorIdAsync(cancellationToken);

        var createSnapshot = new
        {
            building.Id,
            building.FldId,
            building.BuildingName,
            building.StreetName,
            building.HouseNumber,
            building.Neighborhood,
            building.BldSivug,
            building.ShikumStatus,
            building.StatusSummary,
            building.StatusSummaryUpdatedAt
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
            building.BuildingName,
            building.StreetName,
            building.HouseNumber,
            building.Neighborhood,
            building.ShikumStatus,
            building.BldSivug,
            building.StatusSummary));
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

        building.FldId = request.FldId;
        building.StreetName = request.StreetName;
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

        await _context.SaveChangesAsync(cancellationToken);

        var changeSnapshot = new
        {
            building.Id,
            building.FldId,
            building.BuildingName,
            building.StreetName,
            building.HouseNumber,
            building.Neighborhood,
            building.BldSivug,
            building.ShikumStatus,
            building.StatusSummary,
            building.StatusSummaryUpdatedAt
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
