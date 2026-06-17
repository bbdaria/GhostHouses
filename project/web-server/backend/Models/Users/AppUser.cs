using WebServer.Utilities;

namespace WebServer.Models.Users;

public class AppUser
{
    public Guid Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;
    public bool TwoFactorEnabled { get; set; } = true;
    public string TwoFactorSecret { get; set; } = string.Empty;
    public string? PendingTwoFactorCode { get; set; }
    public string? PendingTwoFactorToken { get; set; }
    public DateTimeOffset? PendingTwoFactorExpiry { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = IsraelTime.NowUtc;
    public DateTimeOffset? LastLoginAt { get; set; }

    public ICollection<BuildingLog> Logs { get; set; } = new List<BuildingLog>();
}
