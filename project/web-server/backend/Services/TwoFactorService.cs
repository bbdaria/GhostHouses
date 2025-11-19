using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using WebServer.Models.Users;

namespace WebServer.Services;

public interface ITwoFactorService
{
    (string Code, string Token) IssueCode(AppUser user);
    bool ValidateCode(AppUser user, string code, string token);
}

public class TwoFactorService : ITwoFactorService
{
    private readonly ILogger<TwoFactorService> _logger;

    public TwoFactorService(ILogger<TwoFactorService> logger)
    {
        _logger = logger;
    }

    public (string Code, string Token) IssueCode(AppUser user)
    {
        var code = RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        var token = Guid.NewGuid().ToString("N");
        _logger.LogInformation("2FA code for {Username}: {Code}", user.Username, code);
        return (code, token);
    }

    public bool ValidateCode(AppUser user, string code, string token)
    {
        if (user.PendingTwoFactorCode is null || user.PendingTwoFactorToken is null)
        {
            return false;
        }

        if (!string.Equals(user.PendingTwoFactorToken, token, StringComparison.Ordinal))
        {
            return false;
        }

        if (!string.Equals(user.PendingTwoFactorCode, code, StringComparison.Ordinal))
        {
            return false;
        }

        return user.PendingTwoFactorExpiry is null || user.PendingTwoFactorExpiry > DateTimeOffset.UtcNow;
    }
}
