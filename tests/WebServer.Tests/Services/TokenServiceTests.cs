using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using WebServer.Models.Users;
using WebServer.Services;

namespace WebServer.Tests.Services;

public class TokenServiceTests
{
    private static TokenService CreateService(int expirationMinutes = 60)
    {
        return new TokenService(Options.Create(new JwtOptions
        {
            Issuer = "ghosthouses",
            Audience = "ghosthouses-clients",
            SigningKey = "THIS_IS_A_TEST_SIGNING_KEY_32_CHARS_LONG",
            ExpirationMinutes = expirationMinutes
        }));
    }

    [Fact]
    public void CreateToken_IncludesCurrentUserClaims()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "editor-user",
            Email = "editor@example.com",
            Role = UserRole.Editor
        };

        var token = CreateService().CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("ghosthouses", jwt.Issuer);
        Assert.Contains("ghosthouses-clients", jwt.Audiences);
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == user.Username);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == user.Email);
        Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == UserRole.Editor.ToString());
    }

    [Fact]
    public void CreateToken_UsesConfiguredExpirationWindow()
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "viewer-user",
            Email = "viewer@example.com",
            Role = UserRole.Viewer
        };

        var before = DateTime.UtcNow;
        var token = CreateService(expirationMinutes: 30).CreateToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        var after = DateTime.UtcNow;

        Assert.InRange(jwt.ValidTo, before.AddMinutes(29), after.AddMinutes(31));
    }
}
