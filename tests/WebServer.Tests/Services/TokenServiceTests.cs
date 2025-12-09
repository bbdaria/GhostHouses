using WebServer.Services;
using WebServer.Models.Users;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;

public class TokenServiceTests
{
    private readonly TokenService _service;

    public TokenServiceTests()
    {
        var opts = Options.Create(new JwtOptions
        {
            SigningKey = "THIS_IS_A_TEST_KEY_1234567890",
            Issuer = "ghosthouses",
            Audience = "ghosthouses-clients",
            ExpirationMinutes = 60
        });

        _service = new TokenService(opts);
    }

    [Fact]
    public void CreateToken_IncludesCorrectClaims()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "bayan",
            Email = "bayan@example.com",
            Role = UserRole.Editor
        };

        // Act
        var token = _service.CreateToken(user);

        // Assert
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "bayan");
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Email && c.Value == "bayan@example.com");
        Assert.Contains(jwt.Claims, c => c.Type == System.Security.Claims.ClaimTypes.Role && c.Value == "Editor");
    }
}




[Fact]
public void CreateToken_HasExpiration()
{
    var user = new AppUser
    {
        Id = Guid.NewGuid(),
        Username = "testuser",
        Email = "test@example.com",
        Role = UserRole.Viewer
    };

    var token = _service.CreateToken(user);
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);

    Assert.NotNull(jwt.ValidTo);
    Assert.True(jwt.ValidTo > DateTime.UtcNow);
}
