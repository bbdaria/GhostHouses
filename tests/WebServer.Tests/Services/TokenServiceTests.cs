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




using Microsoft.Extensions.Options;
using WebServer.Services;
using WebServer.Models.Users;
using System.IdentityModel.Tokens.Jwt;

public class TokenServiceTests
{
    [Fact]
    public void CreateToken_ReturnsValidJwt()
    {
        var opts = Options.Create(new JwtOptions
        {
            SigningKey = "THIS_IS_A_TEST_KEY_1234567890",
            Issuer = "test-issuer",
            Audience = "test-audience",
            ExpirationMinutes = 30
        });

        var service = new TokenService(opts);

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Username = "bayan",
            Email = "bayan@test.com",
            Role = UserRole.Admin
        };

        var token = service.CreateToken(user);

        Assert.False(string.IsNullOrWhiteSpace(token));

        // Validate readable JWT
        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        Assert.Equal("bayan", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.UniqueName).Value);
        Assert.Equal("bayan@test.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Contains(jwt.Claims, c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role");
    }
}


[Fact]
public void CreateToken_Returns_NonEmptyString()
{
    var options = Options.Create(new JwtOptions { SigningKey = "secret1234567890" });
    var service = new TokenService(options);
    var user = new AppUser { Id = Guid.NewGuid(), Username = "test", Email = "t@t.com", Role = UserRole.Viewer };

    var token = service.CreateToken(user);

    Assert.False(string.IsNullOrWhiteSpace(token));
}


[Fact]
public void CreateToken_Includes_CorrectClaims()
{
    var options = Options.Create(new JwtOptions { SigningKey = "secret1234567890" });
    var service = new TokenService(options);
    var user = new AppUser { Id = Guid.NewGuid(), Username = "bayan", Email = "b@b.com", Role = UserRole.Editor };

    var token = service.CreateToken(user);

    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);

    Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.Sub && c.Value == user.Id.ToString());
    Assert.Contains(jwt.Claims, c => c.Type == JwtRegisteredClaimNames.UniqueName && c.Value == "bayan");
    Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Email && c.Value == "b@b.com");
    Assert.Contains(jwt.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Editor");
}




[Fact]
public void CreateToken_Has_Expiration()
{
    var options = Options.Create(new JwtOptions { SigningKey = "secret1234567890", ExpirationMinutes = 60 });
    var service = new TokenService(options);
    var user = new AppUser { Id = Guid.NewGuid(), Username = "x", Email = "x@x.com", Role = UserRole.Viewer };

    var token = service.CreateToken(user);

    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);

    Assert.NotNull(jwt.ValidTo);
}







[Fact]
public void CreateToken_ShouldIncludeCorrectUserIdClaim()
{
    // Arrange
    var user = new User { Id = "12345", UserName = "testuser" };

    // Act
    var token = _service.CreateToken(user);
    var handler = new JwtSecurityTokenHandler();
    var jwt = handler.ReadJwtToken(token);

    // Assert
    var claim = jwt.Claims.FirstOrDefault(c => c.Type == "id");
    Assert.NotNull(claim);
    Assert.Equal("12345", claim!.Value);
}



