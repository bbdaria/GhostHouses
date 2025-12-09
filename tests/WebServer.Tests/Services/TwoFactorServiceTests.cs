using WebServer.Services;
using WebServer.Models.Users;
using Microsoft.Extensions.Logging.Abstractions;

public class TwoFactorServiceTests
{
    private readonly TwoFactorService _service;

    public TwoFactorServiceTests()
    {
        _service = new TwoFactorService(new NullLogger<TwoFactorService>());
    }

    [Fact]
    public void IssueCode_ReturnsSixDigitCodeAndToken()
    {
        var user = new AppUser { Username = "bayan" };

        var result = _service.IssueCode(user);

        Assert.NotNull(result.Code);
        Assert.NotNull(result.Token);
        Assert.Equal(6, result.Code.Length);
        Assert.True(int.TryParse(result.Code, out _));
        Assert.False(string.IsNullOrWhiteSpace(result.Token));
    }
}






using WebServer.Services;
using WebServer.Models.Users;
using Microsoft.Extensions.Logging.Abstractions;

public class TwoFactorServiceTests
{
    [Fact]
    public void IssueCode_ReturnsCodeAndToken()
    {
        var service = new TwoFactorService(new NullLogger<TwoFactorService>());

        var user = new AppUser { Username = "bayan" };

        var (code, token) = service.IssueCode(user);

        Assert.False(string.IsNullOrWhiteSpace(code));
        Assert.False(string.IsNullOrWhiteSpace(token));
        Assert.Equal(6, code.Length);
    }







        [Fact]
    public void ValidateCode_ReturnsTrue_ForCorrectValues()
    {
        var service = new TwoFactorService(new NullLogger<TwoFactorService>());

        var user = new AppUser
        {
            PendingTwoFactorCode = "123456",
            PendingTwoFactorToken = "abc123",
            PendingTwoFactorExpiry = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        var result = service.ValidateCode(user, "123456", "abc123");

        Assert.True(result);
    }



        [Fact]
    public void ValidateCode_ReturnsFalse_ForWrongCodeOrToken()
    {
        var service = new TwoFactorService(new NullLogger<TwoFactorService>());

        var user = new AppUser
        {
            PendingTwoFactorCode = "123456",
            PendingTwoFactorToken = "abc123",
            PendingTwoFactorExpiry = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        Assert.False(service.ValidateCode(user, "999999", "abc123"));
        Assert.False(service.ValidateCode(user, "123456", "wrongtoken"));
    }

