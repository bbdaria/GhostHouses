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
