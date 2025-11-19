using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;
using WebServer.Services;

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

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            query = query.Where(b => EF.Functions.ILike(b.ShikumStatus, $"%{filter.Status}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Neighborhood))
        {
            query = query.Where(b => EF.Functions.ILike(b.Neighborhood, $"%{filter.Neighborhood}%"));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(b => b.StreetName)
            .ThenBy(b => b.HouseNumber)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(b => new BuildingSummaryDto(
                b.Id,
                b.BuildingName,
                b.StreetName,
                b.HouseNumber,
                b.Neighborhood,
                b.ShikumStatus,
                b.BldSivug))
            .ToListAsync(cancellationToken);

        return Ok(new PaginatedResult<BuildingSummaryDto>(items, total, filter.Page, filter.PageSize));
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<BuildingDetailDto>> GetBuilding(int id, CancellationToken cancellationToken)
    {
        var building = await _context.Buildings
            .Include(b => b.Logs.OrderByDescending(l => l.CreatedAt))
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (building is null)
        {
            return NotFound();
        }

        var externalData = await _externalDataService.GetBuildingDataAsync(id, cancellationToken);
        var logs = building.Logs
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new BuildingLogDto(l.Id, l.BuildingId, l.Title, l.Message, l.Category, l.Severity, l.CreatedAt, l.CreatedByUser?.Username))
            .ToList();

        var detail = new BuildingDetailDto(
            new BuildingSummaryDto(building.Id, building.BuildingName, building.StreetName, building.HouseNumber, building.Neighborhood, building.ShikumStatus, building.BldSivug),
            building.StatusSummary,
            building.StatusSummaryUpdatedAt,
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
            ShikumStatus = request.ShikumStatus ?? "Unknown",
            StatusSummary = request.StatusSummary ?? string.Empty,
            Complaints = request.Complaints ?? string.Empty,
            PhotoUrls = request.Photos is null ? string.Empty : string.Join(',', request.Photos)
        };

        _context.Buildings.Add(building);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "Create", request, cancellationToken);

        return CreatedAtAction(nameof(GetBuilding), new { id = building.Id }, new BuildingSummaryDto(
            building.Id,
            building.BuildingName,
            building.StreetName,
            building.HouseNumber,
            building.Neighborhood,
            building.ShikumStatus,
            building.BldSivug));
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
        building.BuildingName = request.BuildingName;
        building.Neighborhood = request.Neighborhood;
        building.BldSivug = request.BldSivug ?? building.BldSivug;
        building.ShikumStatus = request.ShikumStatus ?? building.ShikumStatus;
        building.StatusSummary = request.StatusSummary ?? building.StatusSummary;
        building.Complaints = request.Complaints ?? building.Complaints;
        building.PhotoUrls = request.Photos is null ? building.PhotoUrls : string.Join(',', request.Photos);
        building.StatusSummaryUpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(Building), building.Id.ToString(), "Update", request, cancellationToken);
        return NoContent();
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
