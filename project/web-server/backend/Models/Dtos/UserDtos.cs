using System.ComponentModel.DataAnnotations;
using WebServer.Models.Users;

namespace WebServer.Models.Dtos;

public record UserSummaryDto(
    Guid Id,
    string Username,
    string Email,
    UserRole Role,
    bool TwoFactorEnabled,
    DateTimeOffset CreatedAt);

public class CreateUserRequest
{
    [Required]
    public string Username { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public UserRole Role { get; set; } = UserRole.Viewer;
}

public class UpdateUserRequest
{
    [EmailAddress]
    public string? Email { get; set; }
    public UserRole? Role { get; set; }
    public bool? TwoFactorEnabled { get; set; }
}

public class AdminSetPasswordRequest
{
    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = string.Empty;
}
