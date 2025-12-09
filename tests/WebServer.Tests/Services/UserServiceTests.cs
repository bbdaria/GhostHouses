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




    [Fact]
public void VerifyPassword_ReturnsTrue_WhenPasswordMatches()
{
    var service = new UserService();

    var user = new User
    {
        Id = "3",
        UserName = "test2",
        PasswordHash = "mypass"
    };

    var ok = service.VerifyPassword(user, "mypass");

    Assert.True(ok);
}


    [Fact]
public async Task CreateUserAsync_HashesPasswordBeforeStoring()
{
    var service = new UserService();

    var created = await service.CreateUserAsync("bayan", "12345");

    Assert.NotNull(created);
    Assert.NotEqual("12345", created!.PasswordHash);
    Assert.NotEmpty(created.PasswordHash);
}



    [Fact]
public async Task CreateUserAsync_AssignsUniqueId()
{
    var service = new UserService();

    var u1 = await service.CreateUserAsync("user1", "pass");
    var u2 = await service.CreateUserAsync("user2", "pass");

    Assert.NotNull(u1!.Id);
    Assert.NotNull(u2!.Id);
    Assert.NotEqual(u1.Id, u2.Id);
}
