using System.ComponentModel.DataAnnotations;
using WebServer.Models.Users;

namespace WebServer.Models.Dtos;

public class LoginRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public record LoginChallengeResponse(
    Guid UserId,
    bool RequiresTwoFactor,
    string ChallengeToken,
    string DevTwoFactorCode);

public class VerifyTwoFactorRequest
{
    [Required]
    public Guid UserId { get; set; }

    [Required]
    public string ChallengeToken { get; set; } = string.Empty;

    [Required]
    public string Code { get; set; } = string.Empty;
}

public record AuthenticatedUserResponse(
    Guid Id,
    string Username,
    string Email,
    UserRole Role,
    string Token);

public class UpdateCurrentUserRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
}

public class ChangeCurrentPasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
