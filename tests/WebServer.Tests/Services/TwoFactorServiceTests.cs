using Microsoft.Extensions.Logging.Abstractions;
using WebServer.Models.Users;
using WebServer.Services;

namespace WebServer.Tests.Services;

public class TwoFactorServiceTests
{
    private static TwoFactorService CreateService()
    {
        return new TwoFactorService(new NullLogger<TwoFactorService>());
    }

    [Fact]
    public void IssueCode_ReturnsSixDigitCodeAndOpaqueToken()
    {
        var result = CreateService().IssueCode(new AppUser { Username = "admin" });

        Assert.Equal(6, result.Code.Length);
        Assert.True(result.Code.All(char.IsDigit));
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }

    [Fact]
    public void ValidateCode_ReturnsTrue_ForMatchingUnexpiredChallenge()
    {
        var user = new AppUser
        {
            PendingTwoFactorCode = "123456",
            PendingTwoFactorToken = "token",
            PendingTwoFactorExpiry = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        var isValid = CreateService().ValidateCode(user, "123456", "token");

        Assert.True(isValid);
    }

    [Fact]
    public void ValidateCode_ReturnsFalse_ForWrongOrExpiredChallenge()
    {
        var service = CreateService();
        var user = new AppUser
        {
            PendingTwoFactorCode = "123456",
            PendingTwoFactorToken = "token",
            PendingTwoFactorExpiry = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        Assert.False(service.ValidateCode(user, "000000", "token"));
        Assert.False(service.ValidateCode(user, "123456", "wrong-token"));

        user.PendingTwoFactorExpiry = DateTimeOffset.UtcNow.AddMinutes(-1);
        Assert.False(service.ValidateCode(user, "123456", "token"));
    }
}
