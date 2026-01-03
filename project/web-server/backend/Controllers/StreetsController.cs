using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Data;
using WebServer.Models;
using WebServer.Models.Dtos;

namespace WebServer.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StreetsController : ControllerBase
{
    private const int NoStreetId = -1;
    private readonly AppDbContext _context;

    public StreetsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<IEnumerable<StreetDto>>> GetAll([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var query = _context.Streets.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => EF.Functions.ILike(s.Name, $"%{search}%"));
        }

        var items = await query
            .OrderBy(s => s.StreetId == NoStreetId ? 0 : 1)
            .ThenBy(s => s.Name)
            .Select(s => new StreetDto(s.StreetId, s.Name))
            .ToListAsync(cancellationToken);

        return Ok(items);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<StreetDto>> Get(int id, CancellationToken cancellationToken)
    {
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
        if (await _context.Streets.AnyAsync(s => s.StreetId == request.StreetId, cancellationToken))
        {
            return Conflict($"Street with id {request.StreetId} already exists.");
        }

        var street = new Street
        {
            StreetId = request.StreetId,
            Name = request.Name
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

        var street = await _context.Streets.FirstOrDefaultAsync(s => s.StreetId == id, cancellationToken);
        if (street == null)
        {
            return NotFound();
        }

        street.Name = request.Name;
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
}
