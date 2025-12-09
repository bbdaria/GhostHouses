using System.Threading.Tasks;
using WebServer.Models.Users;
using WebServer.Services;
using Xunit;

public class UserServiceTests
{
    private UserService CreateService()
    {
        return new UserService();
    }


    [Fact]
public async Task GetUserByUsername_ReturnsNull_WhenUserDoesNotExist()
{
    var service = new UserService();

    var result = await service.GetUserByUsernameAsync("not-found");

    Assert.Null(result);
}


    [Fact]
public async Task GetUserByUsername_ReturnsUser_WhenUsernameExists()
{
    var service = new UserService();

    var user = new User
    {
        Id = "1",
        UserName = "bayan",
        PasswordHash = "HASHED"
    };

    service.AddUser(user);

    var result = await service.GetUserByUsernameAsync("bayan");

    Assert.NotNull(result);
    Assert.Equal("1", result!.Id);
}



    [Fact]
public void VerifyPassword_ReturnsFalse_WhenPasswordInvalid()
{
    var service = new UserService();

    var user = new User
    {
        Id = "2",
        UserName = "test",
        PasswordHash = "correct123"
    };

    var ok = service.VerifyPassword(user, "wrong");

    Assert.False(ok);
}

