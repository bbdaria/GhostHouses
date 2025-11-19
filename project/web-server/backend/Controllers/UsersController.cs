using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Data;
using WebServer.Models.Dtos;
using WebServer.Models.Users;
using WebServer.Services;

namespace WebServer.Controllers;

[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class UsersController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly IAuditService _auditService;

    public UsersController(
        AppDbContext context,
        IPasswordHasher<AppUser> passwordHasher,
        IAuditService auditService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _auditService = auditService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserSummaryDto>>> GetUsers([FromQuery] string? search = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u => EF.Functions.ILike(u.Username, $"%{search}%") || EF.Functions.ILike(u.Email, $"%{search}%"));
        }

        var users = await query
            .OrderBy(u => u.Username)
            .Select(u => new UserSummaryDto(u.Id, u.Username, u.Email, u.Role, u.TwoFactorEnabled))
            .ToListAsync(cancellationToken);

        return Ok(users);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> GetUser(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        return Ok(new UserSummaryDto(user.Id, user.Username, user.Email, user.Role, user.TwoFactorEnabled));
    }

    [HttpPost]
    public async Task<ActionResult<UserSummaryDto>> CreateUser([FromBody] CreateUserRequest request, CancellationToken cancellationToken)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username, cancellationToken))
        {
            return Conflict("Username already exists.");
        }

        var user = new AppUser
        {
            Username = request.Username,
            Email = request.Email,
            Role = request.Role,
            TwoFactorSecret = Guid.NewGuid().ToString("N")
        };
        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(AppUser), user.Id.ToString(), "Create", request, cancellationToken);

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new UserSummaryDto(user.Id, user.Username, user.Email, user.Role, user.TwoFactorEnabled));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult> UpdateUser(Guid id, [FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        if (request.Email is not null)
        {
            user.Email = request.Email;
        }

        if (request.Role.HasValue)
        {
            user.Role = request.Role.Value;
        }

        if (request.TwoFactorEnabled.HasValue)
        {
            user.TwoFactorEnabled = request.TwoFactorEnabled.Value;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(AppUser), user.Id.ToString(), "Update", request, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/reset-2fa")]
    public async Task<ActionResult> ResetTwoFactor(Guid id, CancellationToken cancellationToken)
    {
        var user = await _context.Users.FindAsync(new object[] { id }, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        user.TwoFactorSecret = Guid.NewGuid().ToString("N");
        user.PendingTwoFactorCode = null;
        user.PendingTwoFactorExpiry = null;
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(CurrentUserId, nameof(AppUser), user.Id.ToString(), "Reset2FA", null, cancellationToken);
        return NoContent();
    }
}
