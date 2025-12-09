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
