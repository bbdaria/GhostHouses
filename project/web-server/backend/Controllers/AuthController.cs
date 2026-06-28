using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WebServer.Data;
using WebServer.Models.Dtos;
using WebServer.Models.Users;
using WebServer.Services;
using WebServer.Utilities;

namespace WebServer.Controllers;

[Route("api/[controller]")]
public class AuthController : ApiControllerBase
{
    private readonly AppDbContext _context;
    private readonly IPasswordHasher<AppUser> _passwordHasher;
    private readonly ITwoFactorService _twoFactorService;
    private readonly ITokenService _tokenService;
    private readonly IAuditService _auditService;

    public AuthController(
        AppDbContext context,
        IPasswordHasher<AppUser> passwordHasher,
        ITwoFactorService twoFactorService,
        ITokenService tokenService,
        IAuditService auditService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _twoFactorService = twoFactorService;
        _tokenService = tokenService;
        _auditService = auditService;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginChallengeResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _context.Users.SingleOrDefaultAsync(u => u.Username == request.Username);
        if (user is null)
        {
            await Task.Delay(250); // mitigate timing attacks
            return Unauthorized();
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return Unauthorized();
        }

        var (code, token) = _twoFactorService.IssueCode(user);
        user.PendingTwoFactorCode = code;
        user.PendingTwoFactorToken = token;
        user.PendingTwoFactorExpiry = IsraelTime.NowUtc.AddMinutes(5);
        await _context.SaveChangesAsync();

        // For Stage A we return the code so the FE can simulate delivery.
        return Ok(new LoginChallengeResponse(user.Id, true, token, code));
    }

    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedUserResponse>> VerifyTwoFactor([FromBody] VerifyTwoFactorRequest request)
    {
        var user = await _context.Users.FindAsync(request.UserId);
        if (user is null)
        {
            return Unauthorized();
        }

        if (!_twoFactorService.ValidateCode(user, request.Code, request.ChallengeToken))
        {
            return Unauthorized("Invalid or expired 2FA code.");
        }

        user.PendingTwoFactorCode = null;
        user.PendingTwoFactorToken = null;
        user.PendingTwoFactorExpiry = null;
        user.LastLoginAt = IsraelTime.NowUtc;
        await _context.SaveChangesAsync();

        var token = _tokenService.CreateToken(user);
        return Ok(new AuthenticatedUserResponse(user.Id, user.Username, user.Email, user.Role, token));
    }

    [HttpGet("me")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<UserSummaryDto>> GetCurrentUser(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            return Unauthorized();
        }

        var user = await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        return Ok(new UserSummaryDto(user.Id, user.Username, user.Email, user.Role, user.TwoFactorEnabled, user.CreatedAt));
    }

    [HttpPut("me")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult<UserSummaryDto>> UpdateCurrentUser(
        [FromBody] UpdateCurrentUserRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAppUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        user.Email = request.Email.Trim();
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(user.Id, nameof(AppUser), user.Id.ToString(), "UpdateOwnProfile", new { request.Email }, cancellationToken);

        return Ok(new UserSummaryDto(user.Id, user.Username, user.Email, user.Role, user.TwoFactorEnabled, user.CreatedAt));
    }

    [HttpPost("me/password")]
    [Authorize(Policy = "Viewer")]
    public async Task<ActionResult> ChangeCurrentPassword(
        [FromBody] ChangeCurrentPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var user = await GetCurrentAppUser(cancellationToken);
        if (user is null)
        {
            return Unauthorized();
        }

        var passwordResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (passwordResult == PasswordVerificationResult.Failed)
        {
            return BadRequest("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(user, request.NewPassword);
        await _context.SaveChangesAsync(cancellationToken);
        await _auditService.RecordAsync(user.Id, nameof(AppUser), user.Id.ToString(), "ChangeOwnPassword", null, cancellationToken);

        return NoContent();
    }

    private async Task<AppUser?> GetCurrentAppUser(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId;
        if (userId is null)
        {
            return null;
        }

        return await _context.Users.FindAsync(new object[] { userId.Value }, cancellationToken);
    }
}
