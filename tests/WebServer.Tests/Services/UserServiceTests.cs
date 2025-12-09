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


    [Fact]
public async Task CreateUserAsync_Throws_WhenUsernameAlreadyExists()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "123");

    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await service.CreateUserAsync("bayan", "999");
    });
}


    [Fact]
public async Task AuthenticateUserAsync_ReturnsNull_WhenUserNotFound()
{
    var service = new UserService();

    var result = await service.AuthenticateUserAsync("ghost", "1234");

    Assert.Null(result);
}


    

    [Fact]
public async Task AuthenticateUserAsync_ReturnsNull_ForWrongPassword()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "correct");

    var result = await service.AuthenticateUserAsync("bayan", "wrong");

    Assert.Null(result);
}



    [Fact]
public async Task AuthenticateUserAsync_ReturnsUser_WhenCredentialsAreCorrect()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "1234");

    var result = await service.AuthenticateUserAsync("bayan", "1234");

    Assert.NotNull(result);
    Assert.Equal("bayan", result!.Username);
}



    [Fact]
public async Task DeleteUserAsync_ReturnsFalse_WhenUserDoesNotExist()
{
    var service = new UserService();

    var result = await service.DeleteUserAsync("ghost");

    Assert.False(result);
}


[Fact]
public async Task DeleteUserAsync_DeletesExistingUser()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "1234");

    var result = await service.DeleteUserAsync("bayan");

    Assert.True(result);
}


    [Fact]
public async Task DeleteUserAsync_RemovesUserFromStore()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "1234");

    await service.DeleteUserAsync("bayan");

    var auth = await service.AuthenticateUserAsync("bayan", "1234");

    Assert.Null(auth);
}


    [Fact]
public async Task GetAllUsers_ReturnsEmpty_WhenNoUsersExist()
{
    var service = new UserService();

    var users = await service.GetAllUsersAsync();

    Assert.Empty(users);
}


    [Fact]
public async Task GetAllUsers_ReturnsAllCreatedUsers()
{
    var service = new UserService();

    await service.CreateUserAsync("u1", "p1");
    await service.CreateUserAsync("u2", "p2");

    var users = await service.GetAllUsersAsync();

    Assert.Equal(2, users.Count());
}




    [Fact]
public async Task GetAllUsers_ReturnsCorrectUsernames()
{
    var service = new UserService();

    await service.CreateUserAsync("alice", "123");
    await service.CreateUserAsync("bob", "456");

    var users = await service.GetAllUsersAsync();
    var names = users.Select(u => u.Username).ToList();

    Assert.Contains("alice", names);
    Assert.Contains("bob", names);
}


    [Fact]
public async Task CreateUserAsync_Throws_WhenUsernameExists()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "12345");

    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await service.CreateUserAsync("bayan", "another");
    });
}


[Fact]
public async Task DeleteUserAsync_RemovesUser_WhenExists()
{
    var service = new UserService();

    var user = await service.CreateUserAsync("temp", "pw");
    await service.DeleteUserAsync(user.Id);

    var users = await service.GetAllUsersAsync();

    Assert.DoesNotContain(users, u => u.Id == user.Id);
}



[Fact]
public async Task DeleteUserAsync_DoesNothing_WhenUserDoesNotExist()
{
    var service = new UserService();

    // no users created
    await service.DeleteUserAsync(Guid.NewGuid());

    var users = await service.GetAllUsersAsync();

    Assert.Empty(users); // should stay empty
}



    [Fact]
public async Task GetAllUsersAsync_ReturnsEmpty_WhenNoUsers()
{
    var service = new UserService();

    var result = await service.GetAllUsersAsync();

    Assert.Empty(result);
}



    [Fact]
public async Task GetAllUsersAsync_ReturnsAllCreatedUsers()
{
    var service = new UserService();

    await service.CreateUserAsync("u1", "p1");
    await service.CreateUserAsync("u2", "p2");

    var result = await service.GetAllUsersAsync();

    Assert.Equal(2, result.Count);
    Assert.Contains(result, u => u.Username == "u1");
    Assert.Contains(result, u => u.Username == "u2");
}


    [Fact]
public async Task GetUserByIdAsync_ReturnsNull_WhenUserDoesNotExist()
{
    var service = new UserService();

    var result = await service.GetUserByIdAsync(Guid.NewGuid());

    Assert.Null(result);
}

[Fact]
public async Task CreateUserAsync_StoresHashedPassword()
{
    var service = new UserService();

    var user = await service.CreateUserAsync("bayan", "mypassword");

    Assert.NotEqual("mypassword", user.PasswordHash);
    Assert.False(string.IsNullOrWhiteSpace(user.PasswordHash));
}


    [Fact]
public async Task CreateUserAsync_Throws_WhenUsernameExists()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "123");

    await Assert.ThrowsAsync<InvalidOperationException>(async () =>
    {
        await service.CreateUserAsync("bayan", "otherpass");
    });
}



    [Fact]
public async Task AuthenticateAsync_ReturnsNull_WhenPasswordIncorrect()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "correctpass");

    var result = await service.AuthenticateAsync("bayan", "wrongpass");

    Assert.Null(result);
}


    [Fact]
public async Task AuthenticateAsync_ReturnsUser_WhenPasswordCorrect()
{
    var service = new UserService();

    await service.CreateUserAsync("bayan", "pass123");

    var result = await service.AuthenticateAsync("bayan", "pass123");

    Assert.NotNull(result);
    Assert.Equal("bayan", result!.Username);
}


